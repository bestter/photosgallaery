using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PhotoAppApi
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class RequireWebsiteHeaderAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var request = context.HttpContext.Request;

            // On vérifie si l'en-tête "X-App-Client" est présent et contient la bonne valeur secrète
            if (!request.Headers.TryGetValue("X-App-Client", out var clientType) || clientType != "PhotoApp-Web")
            {
                // Si ce n'est pas le cas, on rejette la requête immédiatement. 
                // Ça bloque l'accès via Postman, Navigateur direct ou Scripts qui n'imitent pas parfaitement l'URL.
                context.Result = new BadRequestObjectResult(new { message = "Accès refusé. Seules les requêtes provenant du site web officiel sont autorisées." });
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
