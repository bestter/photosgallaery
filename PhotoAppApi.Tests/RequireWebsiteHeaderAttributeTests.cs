using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace PhotoAppApi.Tests
{
    public class RequireWebsiteHeaderAttributeTests
    {
        [Fact]
        public void OnActionExecuting_MissingHeader_ReturnsBadRequest()
        {
            // Arrange
            var attribute = new RequireWebsiteHeaderAttribute();
            var context = CreateContext();

            // Act
            attribute.OnActionExecuting(context);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(context.Result);
            var value = badRequestResult.Value;
            var message = value?.GetType().GetProperty("message")?.GetValue(value, null);
            Assert.Equal("Accès refusé. Seules les requêtes provenant du site web officiel sont autorisées.", message);
        }

        [Fact]
        public void OnActionExecuting_InvalidHeaderValue_ReturnsBadRequest()
        {
            // Arrange
            var attribute = new RequireWebsiteHeaderAttribute();
            var context = CreateContext();
            context.HttpContext.Request.Headers["X-App-Client"] = "Some-Other-Client";

            // Act
            attribute.OnActionExecuting(context);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(context.Result);
            var value = badRequestResult.Value;
            var message = value?.GetType().GetProperty("message")?.GetValue(value, null);
            Assert.Equal("Accès refusé. Seules les requêtes provenant du site web officiel sont autorisées.", message);
        }

        [Fact]
        public void OnActionExecuting_ValidHeaderValue_DoesNotSetResult()
        {
            // Arrange
            var attribute = new RequireWebsiteHeaderAttribute();
            var context = CreateContext();
            context.HttpContext.Request.Headers["X-App-Client"] = "PhotoApp-Web";

            // Act
            attribute.OnActionExecuting(context);

            // Assert
            Assert.Null(context.Result);
        }

        private static ActionExecutingContext CreateContext()
        {
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(
                httpContext,
                new RouteData(),
                new ActionDescriptor()
            );

            return new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                new Mock<Controller>().Object
            );
        }
    }
}
