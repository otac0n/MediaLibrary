namespace MediaLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using MediaLibrary.Storage;
    using MediaLibrary.Storage.Search;

    public partial class CompareForm : Form
    {
        private readonly string category;
        private readonly MediaIndex index;
        private readonly Random random;
        private readonly Dictionary<string, long> ratingsInverseFrequency;
        private readonly List<SearchResult> searchResults;
        private (ItemInfo left, ItemInfo right)? compareInfo;

        public CompareForm(MediaIndex index, string category, IEnumerable<SearchResult> searchResults)
        {
            this.index = index;
            this.category = category;
            this.searchResults = searchResults.ToList();
            this.ratingsInverseFrequency = string.IsNullOrEmpty(category)
                ? this.searchResults.ToDictionary(r => r.Hash, r => (r.Rating?.Count ?? 0) + 1)
                : this.searchResults.ToDictionary(r => r.Hash, r => 1L);
            this.random = new Random();
            this.InitializeComponent();
            this.LoadNextComparison();
        }

        private enum Mode
        {
            Uniform,
            PickByFrequency,
        }

        private (ItemInfo left, ItemInfo right)? CompareInfo
        {
            get
            {
                return this.compareInfo;
            }

            set
            {
                this.compareInfo = value;
                if (value is null)
                {
                    this.leftPreview.PreviewItems = this.rightPreview.PreviewItems = Array.Empty<SearchResult>();
                }
                else
                {
                    var (left, right) = value.Value;
                    this.leftPreview.PreviewItems = new[] { this.searchResults[left.Index] };
                    this.rightPreview.PreviewItems = new[] { this.searchResults[right.Index] };
                }
            }
        }

        private static double GetExpected(ItemInfo left, ItemInfo right) =>
            1.0 / (1.0 + Math.Pow(10.0, (left.Rating.Value - right.Rating.Value) / 400.0));

        private async Task LoadNextComparison(int? leftIndex = null, int? rightIndex = null)
        {
            int RandomIndex(Mode mode = Mode.PickByFrequency, int? avoid = null)
            {
                switch (mode)
                {
                    case Mode.Uniform:
                        {
                            if (avoid == null)
                            {
                                return this.random.Next(this.searchResults.Count);
                            }

                            var ix = this.random.Next(this.searchResults.Count - 1);
                            if (ix >= avoid)
                            {
                                ix += 1;
                            }

                            return ix;
                        }

                    case Mode.PickByFrequency:
                        {
                            var indices = Enumerable.Range(0, this.searchResults.Count);
                            if (avoid is int avoidVavlue)
                            {
                                indices = indices.Where(i => i != avoidVavlue);
                            }

                            var power = indices.Select(i => (index: i, power: 1.0 / this.ratingsInverseFrequency[this.searchResults[i].Hash])).OrderBy(i => i.power).ToList();
                            var total = power.Sum(p => p.power);

                            var target = this.random.NextDouble() * total;
                            var acc = 0.0;
                            for (var i = 0; i < power.Count - 1; i++)
                            {
                                acc += power[i].power;
                                if (acc >= target)
                                {
                                    return power[i].index;
                                }
                            }

                            return power[power.Count - 1].index;
                        }
                }

                throw new NotImplementedException();
            }

            int leftIx, rightIx;
            if (leftIndex == null)
            {
                if (rightIndex == null)
                {
                    leftIx = RandomIndex();
                    rightIx = RandomIndex(mode: Mode.Uniform, avoid: leftIx);
                }
                else
                {
                    rightIx = rightIndex.Value;
                    leftIx = RandomIndex(avoid: rightIx);
                }
            }
            else
            {
                leftIx = leftIndex.Value;
                rightIx = RandomIndex(avoid: leftIx);
            }

            var leftResult = this.searchResults[leftIx];
            var rightResult = this.searchResults[rightIx];
            var leftRating = await this.index.GetRating(leftResult.Hash, this.category).ConfigureAwait(true) ?? new Rating(leftResult.Hash, this.category, Rating.DefaultRating, 0);
            var rightRating = await this.index.GetRating(rightResult.Hash, this.category).ConfigureAwait(true) ?? new Rating(rightResult.Hash, this.category, Rating.DefaultRating, 0);
            var left = new ItemInfo(leftIx, leftRating);
            var right = new ItemInfo(rightIx, rightRating);
            var expected = GetExpected(left, right);

            this.CompareInfo = (left, right);
            this.ratingBar.Value = this.ratingBar.Minimum + (int)Math.Round(expected * (this.ratingBar.Maximum - this.ratingBar.Minimum));
            this.ratingBar.Focus();
        }

        private async void RateButton_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            try
            {
                var (left, right) = this.CompareInfo.Value;
                var expected = GetExpected(left, right);
                var actual = (double)(this.ratingBar.Value - this.ratingBar.Minimum) / (this.ratingBar.Maximum - this.ratingBar.Minimum);
                var averageCount = (left.Rating.Count + right.Rating.Count) / 2.0;
                var k = 15 + 15.0 / (0.14 * averageCount + 1);

                var leftRating = new Rating(
                    left.Rating.Hash,
                    left.Rating.Category,
                    left.Rating.Value + k * (expected - actual),
                    left.Rating.Count + 1);

                var rightRating = new Rating(
                    right.Rating.Hash,
                    right.Rating.Category,
                    right.Rating.Value + k * (actual - expected),
                    right.Rating.Count + 1);

                await this.index.UpdateRating(leftRating).ConfigureAwait(true);
                await this.index.UpdateRating(rightRating).ConfigureAwait(true);
                this.ratingsInverseFrequency[leftRating.Hash]++;
                this.ratingsInverseFrequency[rightRating.Hash]++;
                await this.LoadNextComparison(
                    leftIndex: this.ratingBar.Value == this.ratingBar.Minimum ? left.Index : default(int?),
                    rightIndex: this.ratingBar.Value == this.ratingBar.Maximum ? right.Index : default(int?)).ConfigureAwait(true);
            }
            finally
            {
                this.Enabled = true;
            }
        }

        private void SkipButton_Click(object sender, EventArgs e)
        {
            this.LoadNextComparison();
        }

        private struct ItemInfo
        {
            public ItemInfo(int index, Rating rating)
            {
                this.Index = index;
                this.Rating = rating;
            }

            public int Index { get; }

            public Rating Rating { get; }
        }
    }
}
