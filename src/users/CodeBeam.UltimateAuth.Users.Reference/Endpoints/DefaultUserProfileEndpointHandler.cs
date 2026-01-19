using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeBeam.UltimateAuth.Users.Reference.Endpoints
{
    public sealed class DefaultUserProfileEndpointHandler : IUserProfileEndpointHandler
    {
        private readonly IUAuthUserProfileService _profiles;

        public Task<IResult> GetAsync(HttpContext ctx)
        {
            throw new NotImplementedException();
        }

        //public Task<IResult> GetAsync(HttpContext ctx)
        //=> Results.Ok(await _profiles.GetCurrentAsync());

        public async Task<IResult> UpdateAsync(HttpContext ctx)
        {
            //var req = await ctx.ReadJsonAsync<UpdateProfileRequest>();
            //await _profiles.UpdateProfileAsync(...);
            return Results.NoContent();
        }
    }

}
