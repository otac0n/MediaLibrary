// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Web.Controllers
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Threading.Tasks;
    using MediaLibrary.Storage;
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("ratings")]
    public class RatingsController : ControllerBase
    {
        private readonly MediaIndex index;

        public RatingsController(MediaIndex index)
        {
            this.index = index;
        }

        [Route("categories")]
        [HttpGet]
        public Task<List<string>> Get()
        {
            return this.index.GetAllRatingCategories();
        }

        [Route("categories/{category}/files/{id}")]
        [HttpGet]
        public Task<Rating> Get(string id, string category)
        {
            ControllerUtilities.FixSlashes(ref category);

            return this.index.GetRating(id, category);
        }

        [Route("files/{id}")]
        [HttpGet]
        public Task<Rating> GetDefault(string id) => this.Get(id, null);

        [Route("stars")]
        [HttpGet]
        public async Task<IEnumerable<double?[]>> GetRatingStarRanges() => (await this.index.GetRatingStarRanges().ConfigureAwait(true)).Select(r => new[] { r.min, r.max });

        [Route("categories/{category}/rate")]
        [HttpPost]
        public async Task<Rating[]> Rate(string category, [FromBody] RateRequest rateRequest)
        {
            ControllerUtilities.FixSlashes(ref category);

            var leftRating = await this.index.GetRating(rateRequest.LeftHash, category).ConfigureAwait(true) ?? new Rating(rateRequest.LeftHash, category, Rating.DefaultRating, 0);
            var rightRating = await this.index.GetRating(rateRequest.RightHash, category).ConfigureAwait(true) ?? new Rating(rateRequest.RightHash, category, Rating.DefaultRating, 0);
            Rating.ApplyScore(rateRequest.Score, ref leftRating, ref rightRating);
            await this.index.UpdateRating(leftRating).ConfigureAwait(true);
            await this.index.UpdateRating(rightRating).ConfigureAwait(true);
            return new[] { leftRating, rightRating };
        }

        [Route("rate")]
        [HttpPost]
        public Task<Rating[]> RateDefault([FromBody] RateRequest rateRequest) => this.Rate(null, rateRequest);

        public class RateRequest
        {
            public string LeftHash { get; set; }

            public string RightHash { get; set; }

            [Range(0.0, 1.0)]
            public double Score { get; set; }
        }
    }
}
