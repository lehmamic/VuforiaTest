﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ClimbingApp.Routes.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents.Session;

namespace ClimbingApp.Routes.Controllers.ClimbingRoutes
{
    [Route("api/v1/sites/{siteId}/[controller]")]
    [ApiController]
    public class RoutesController : ControllerBase
    {
        private readonly IAsyncDocumentSession documentSession;
        private readonly IMapper mapper;

        public RoutesController(IAsyncDocumentSession documentSession, IMapper mapper)
        {
            this.documentSession = documentSession ?? throw new System.ArgumentNullException(nameof(documentSession));
            this.mapper = mapper ?? throw new System.ArgumentNullException(nameof(mapper));
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<ClimbingRouteResponse>>> Get([FromRoute]string siteId)
        {
            ClimbingSite site = await this.documentSession.LoadAsync<ClimbingSite>(siteId);
            if (site == null)
            {
                return NotFound();
            }

            var response = this.mapper.Map<IEnumerable<ClimbingRouteResponse>>(site.Routes);
            return Ok(response);
        }

        [HttpGet("{id}", Name = "GetClimbingRoute")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ClimbingRouteResponse>> Get([FromRoute]string siteId, [FromRoute]string id)
        {
            ClimbingSite site = await this.documentSession.LoadAsync<ClimbingSite>(siteId);
            if (site == null)
            {
                return NotFound();
            }

            ClimbingRoute route = site.Routes.FirstOrDefault(r => string.Equals(r.Id, id, StringComparison.InvariantCultureIgnoreCase));
            if (route == null)
            {
                return NotFound();
            }

            var response = this.mapper.Map<ClimbingRouteResponse>(route);
            return Ok(response);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ClimbingRouteResponse>> Post([FromRoute]string siteId, [FromBody] CreateClimbingRouteRequest value)
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            ClimbingSite site = await this.documentSession.LoadAsync<ClimbingSite>(siteId);
            if (site == null)
            {
                return NotFound();
            }

            ClimbingRoute route = this.mapper.Map<ClimbingRoute>(value);
            site.Routes.Add(route);

            await this.documentSession.SaveChangesAsync();

            var response = this.mapper.Map<ClimbingRouteResponse>(route);
            return CreatedAtRoute("GetClimbingRoute", new { siteId, id = response.Id }, response);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> Put([FromRoute]string siteId, [FromRoute]string id, [FromBody] UpdateClimbingRouteRequest value)
        {
            if (!this.ModelState.IsValid)
            {
                return this.BadRequest(this.ModelState);
            }

            ClimbingSite site = await this.documentSession.LoadAsync<ClimbingSite>(siteId);
            if (site == null)
            {
                return NotFound();
            }

            ClimbingRoute route = site.Routes.FirstOrDefault(r => string.Equals(r.Id, id, StringComparison.InvariantCultureIgnoreCase));
            if (route == null)
            {
                return NotFound();
            }

            this.mapper.Map(value, route);
            await this.documentSession.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> Delete([FromRoute]string siteId, [FromRoute]string id)
        {
            ClimbingSite site = await this.documentSession.LoadAsync<ClimbingSite>(siteId);
            if (site == null)
            {
                return NoContent();
            }

            ClimbingRoute route = site.Routes.FirstOrDefault(r => string.Equals(r.Id, id, StringComparison.InvariantCultureIgnoreCase));
            if (route != null)
            {
                site.Routes.Remove(route);
                await this.documentSession.SaveChangesAsync();
            }

            return NoContent();
        }
    }
}