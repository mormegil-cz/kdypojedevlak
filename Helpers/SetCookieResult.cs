using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KdyPojedeVlak.Helpers
{
    /**
     * A helper ActionResult allowing to set a cookie in addition to another action result
     */
    public class SetCookieResult : ActionResult
    {
        private readonly string cookieKey;
        private readonly string cookieValue;
        private readonly CookieOptions cookieOptions;
        private readonly ActionResult baseResult;

        public SetCookieResult(string cookieKey, string cookieValue, CookieOptions cookieOptions, ActionResult baseResult)
        {
            this.cookieKey = cookieKey;
            this.cookieValue = cookieValue;
            this.cookieOptions = cookieOptions;
            this.baseResult = baseResult;
        }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.Cookies.Append(cookieKey, cookieValue, cookieOptions);
            return baseResult.ExecuteResultAsync(context);
        }

        public override void ExecuteResult(ActionContext context)
        {
            context.HttpContext.Response.Cookies.Append(cookieKey, cookieValue, cookieOptions);
            baseResult.ExecuteResult(context);
        }
    }
}
