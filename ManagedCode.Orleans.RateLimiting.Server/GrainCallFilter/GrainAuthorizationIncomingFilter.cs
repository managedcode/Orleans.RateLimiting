// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Reflection;
// using System.Threading.Tasks;
// using ManagedCode.Orleans.RateLimiting.Core.Extensions;
// using ManagedCode.Orleans.RateLimiting.Core.Interfaces;
// using Microsoft.AspNetCore.Authorization;
// using Orleans;
//
// namespace ManagedCode.Orleans.RateLimiting.Server.GrainCallFilter;
//
// public class GrainAuthorizationIncomingFilter : IIncomingGrainCallFilter
// {
//     private readonly IClusterClient _client;
//     private readonly IGrainFactory _grainFactory;
//
//     public GrainAuthorizationIncomingFilter(IClusterClient client, IGrainFactory grainFactory)
//     {
//         _client = client;
//         _grainFactory = grainFactory;
//     }
//
//     public async Task Invoke(IIncomingGrainCallContext context)
//     {
//         if (IsGrainAuthorized(context.ImplementationMethod, out var attributes))
//         {
//             var isSessionExists = await IsAuthorized();
//             if (isSessionExists)
//             {
//                 if (attributes.All(attributes => string.IsNullOrWhiteSpace(attributes.Roles)))
//                 {
//                     await context.Invoke();
//                     return;
//                 }
//                 var roles = this.GetOrleansContext().ToHashSet();
//                 foreach (var attribute in attributes)
//                 {
//                     var intersect = attribute.Roles?.Split(',') ?? Array.Empty<string>();
//                     if (intersect.Any(role => roles.Contains(role)))
//                     {
//                         await context.Invoke();
//                         return;
//                     }
//                 }
//             }
//             throw new UnauthorizedAccessException();
//         }
//         else
//         {
//             await context.Invoke();
//         }
//     }
//
//     private async Task<bool> IsAuthorized()
//     {
//         var sessionId = this.GetSessionId();
//         if (string.IsNullOrWhiteSpace(sessionId) is false)
//         {
//             var sessionGrain = _grainFactory.GetGrain<ISessionGrain>(sessionId);
//             var result = await sessionGrain.ValidateAndGetClaimsAsync();
//             return result.IsSuccess;
//         }
//         return false;
//     }
//
//     private static bool IsGrainAuthorized(MemberInfo methodInfo, out List<AuthorizeAttribute> attributes)
//     {
//         attributes = new List<AuthorizeAttribute>();
//
//         // for method
//         if (Attribute.IsDefined(methodInfo, typeof(AllowAnonymousAttribute)))
//         {
//             return false;
//         }
//
//         if (methodInfo.DeclaringType != null && Attribute.IsDefined(methodInfo.DeclaringType, typeof(AuthorizeAttribute)))
//         {
//             attributes.AddRange(Attribute.GetCustomAttributes(methodInfo.DeclaringType, typeof(AuthorizeAttribute)).Select(s => (AuthorizeAttribute)s));
//         }
//
//         if (Attribute.IsDefined(methodInfo, typeof(AuthorizeAttribute)))
//         {
//             attributes.AddRange(Attribute.GetCustomAttributes(methodInfo, typeof(AuthorizeAttribute)).Select(s => (AuthorizeAttribute)s));
//             return true;
//         }
//
//         return attributes.Any();
//     }
// }

