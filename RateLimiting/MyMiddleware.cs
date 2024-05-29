using Microsoft.Extensions.Caching.Memory;

namespace RateLimiting
{
    public class MyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _timeWindow = TimeSpan.FromSeconds(10);
        private readonly string _cacheKey = "GlobalRateLimit";
        private readonly int _maxRequests = 3;

        public MyMiddleware(RequestDelegate next, IMemoryCache cache)
        {
            _next = next;
            _cache = cache;
        }

        public async Task Invoke(HttpContext context)
        {
            var requestCount = _cache.GetOrCreate(_cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _timeWindow;
                return 0;
            });

            if (requestCount >= _maxRequests)
            {
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync("Too many requests. Please try again later.");
                return;
            }

            _cache.Set(_cacheKey, requestCount + 1, _timeWindow);

            await _next.Invoke(context);
        }
    }
}

