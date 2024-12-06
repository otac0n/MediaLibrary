namespace MediaLibrary.Views
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using MediaLibrary.Components;
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
        private PreviewControl leftPreview;
        private PreviewControl rightPreview;

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
            this.ratingBar.SeekOnClick();

            this.leftPreview = new PreviewControl(index)
            {
                BackColor = System.Drawing.Color.Black,
                Dock = DockStyle.Fill,
                Name = "leftPreview",
                TabIndex = 0,
            };

            this.rightPreview = new PreviewControl(index)
            {
                BackColor = System.Drawing.Color.Black,
                Dock = DockStyle.Fill,
                Name = "rightPreview",
                TabIndex = 1,
            };

            this.previewTable.Controls.Add(this.leftPreview, 0, 0);
            this.previewTable.Controls.Add(this.rightPreview, 1, 0);

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

        private async Task LoadNextComparison(int? leftIndex = null, int? rightIndex = null)
        {
            int RandomIndexUniform(int? avoid = null)
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

            int RandomIndexInverseRatings(int? avoid = null)
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

            bool RandomBit() => this.random.Next(2) > 0;

            (int, int) PickNext()
            {
                int leftIx, rightIx;

                if (leftIndex == null)
                {
                    if (rightIndex == null)
                    {
                        leftIx = RandomIndexInverseRatings();
                        rightIx = RandomIndexUniform(avoid: leftIx);
                    }
                    else
                    {
                        rightIx = rightIndex.Value;
                        leftIx = RandomIndexInverseRatings(avoid: rightIx);
                    }
                }
                else
                {
                    leftIx = leftIndex.Value;
                    rightIx = RandomIndexInverseRatings(avoid: leftIx);
                }

                return (leftIx, rightIx);
            }

            (int, SearchResult, int, SearchResult) GetNextResults()
            {
                int leftIx, rightIx;
                SearchResult leftResult, rightResult;
                bool rejected;
                do
                {
                    (leftIx, rightIx) = PickNext();
                    leftResult = this.searchResults[leftIx];
                    rightResult = this.searchResults[rightIx];

                    var people = leftResult.People.Count;
                    var tags = leftResult.Tags.Count;
                    var total = people + tags;

                    rejected = RandomBit();

                    if (rejected && total > 0)
                    {
                        var personOrTag = Random.Shared.Next(total);
                        if (personOrTag < people)
                        {
                            var person = leftResult.People.ToArray()[personOrTag];
                            if (!rightResult.People.Contains(person))
                            {
                                rejected = false;
                            }
                        }
                        else
                        {
                            personOrTag -= people;
                            var tag = leftResult.Tags.ToArray()[personOrTag];
                            if (!rightResult.Tags.Contains(tag))
                            {
                                rejected = false;
                            }
                        }
                    }
                }
                while (rejected);

                return (leftIx, leftResult, rightIx, rightResult);
            }

            var (leftIx, leftResult, rightIx, rightResult) = GetNextResults();
            var leftRating = await this.index.GetRating(leftResult.Hash, this.category).ConfigureAwait(true) ?? new Rating(leftResult.Hash, this.category, Rating.DefaultRating, 0);
            var rightRating = await this.index.GetRating(rightResult.Hash, this.category).ConfigureAwait(true) ?? new Rating(rightResult.Hash, this.category, Rating.DefaultRating, 0);
            var left = new ItemInfo(leftIx, leftRating);
            var right = new ItemInfo(rightIx, rightRating);
            var expected = Rating.GetExpectedScore(leftRating, rightRating);

            this.CompareInfo = (left, right);
            if (!this.IsDisposed)
            {
                this.ratingBar.Value = this.ratingBar.Minimum + (int)Math.Round(expected * (this.ratingBar.Maximum - this.ratingBar.Minimum));
                this.ratingBar.Focus();
            }
        }

        private async void RateButton_Click(object sender, EventArgs e)
        {
            this.Enabled = false;
            try
            {
                var (left, right) = this.CompareInfo.Value;
                var leftRating = left.Rating;
                var rightRating = right.Rating;

                var actual = (double)(this.ratingBar.Value - this.ratingBar.Minimum) / (this.ratingBar.Maximum - this.ratingBar.Minimum);
                Rating.ApplyScore(actual, ref leftRating, ref rightRating);

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

        private async void SkipButton_Click(object sender, EventArgs e)
        {
            await this.LoadNextComparison();
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
