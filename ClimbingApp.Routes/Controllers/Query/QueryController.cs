﻿using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ClimbingApp.Routes.Controllers.ClimbingRoutes;
using ClimbingApp.Routes.Entities;
using ClimbingApp.Routes.Services.ImageRecognition;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace ClimbingApp.Routes.Controllers.Query
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class QueryController : ControllerBase
    {
        private readonly IAsyncDocumentSession documentSession;
        private readonly IMapper mapper;
        private readonly IImageRecognitionApiClient imageRecognition;
        private readonly ILogger<QueryController> logger;

        public QueryController(IAsyncDocumentSession documentSession, IMapper mapper, IImageRecognitionApiClient imageRecognition, ILogger<QueryController> logger)
        {
            this.documentSession = documentSession ?? throw new System.ArgumentNullException(nameof(documentSession));
            this.mapper = mapper ?? throw new System.ArgumentNullException(nameof(mapper));
            this.imageRecognition = imageRecognition ?? throw new System.ArgumentNullException(nameof(imageRecognition));
            this.logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        public async Task<ActionResult> Query([FromBody]QueryRequest query)
        {
            QueryResponse result = new QueryResponse();

            Services.ImageRecognition.QueryResponse response = await this.imageRecognition.Query(query.Image.Base64);
            QueryResult match = response.Results.OrderByDescending(r => r.Score).FirstOrDefault(r => r.Score > 0.85);
            if (match != null)
            {
                this.logger.LogInformation("Found a matching target");

                match.Target.Labels.TryGetValue(ClimbingRoutesConstants.CLIMBING_ROUTE_ID_LABEL, out string climbingRouteId);
                ClimbingSite site = await this.documentSession.Query<ClimbingSite>()
                    .FirstOrDefaultAsync(s => s.Routes.Any(r => r.Id == climbingRouteId));

                if (site != null)
                {
                    ClimbingRoute route = site.Routes.Single(r => r.Id == climbingRouteId);

                    result.Result = QueryResultType.Match;
                    result.ClimbingRoute = this.mapper.Map<ClimbingRouteMatch>(route);
                    result.ClimbingRoute.Site = this.mapper.Map<ClimbingSiteMatch>(site);

                    return Ok(result);
                }
            }

            result.Result = QueryResultType.NoMatch;
            result.ClimbingRoute = new ClimbingRouteMatch()
            {
                Site = new ClimbingSiteMatch(),
            };

            return Ok(result);
        }
    }
}
