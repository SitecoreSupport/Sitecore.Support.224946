using Sitecore.Forms.Mvc;
using System;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Web.Caching;
using System.Web.Mvc;

namespace Sitecore.Forms.Mvc.Controllers.Filters
{
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
  internal class WffmLimitMultipleSubmitsAttribute : FilterAttribute, IAuthorizationFilter
  {
    public void OnAuthorization(AuthorizationContext filterContext)
    {
      Cache cache = filterContext.HttpContext.Cache;
      if (!string.IsNullOrEmpty(filterContext.HttpContext.Request.Headers["X-RequestVerificationToken"]))
      {
        int limitMultipleSubmits_IntervalInSeconds = Settings.LimitMultipleSubmits_IntervalInSeconds;
        if (limitMultipleSubmits_IntervalInSeconds > 0)
        {
          string input = filterContext.HttpContext.Request.Headers["X-RequestVerificationToken"];
          input = CalculateMD5Hash(input);
          DateTime now;
          if (cache[input] != null)
          {
            if (cache[input] == string.Empty)
            {
              cache.Remove(input);
              Cache cache2 = cache;
              string key = input;
              now = DateTime.Now;
              cache2.Add(key, "attempted", null, now.AddSeconds((double)limitMultipleSubmits_IntervalInSeconds), Cache.NoSlidingExpiration, CacheItemPriority.Normal, null);
              throw new SecurityException("There was an attempt to do multiple submits within a time interval, specified in the \"WFM.LimitMultipleSubmits.IntervalInSeconds\" setting!");
            }
            filterContext.Result = new HttpUnauthorizedResult();
          }
          else
          {
            Cache cache3 = cache;
            string key2 = input;
            now = DateTime.Now;
            cache3.Add(key2, "", null, now.AddSeconds((double)limitMultipleSubmits_IntervalInSeconds), Cache.NoSlidingExpiration, CacheItemPriority.Normal, null);
          }
        }
      }
    }

    public string CalculateMD5Hash(string input)
    {
      MD5 mD = MD5.Create();
      byte[] bytes = Encoding.ASCII.GetBytes(input);
      byte[] array = mD.ComputeHash(bytes);
      StringBuilder stringBuilder = new StringBuilder();
      for (int i = 0; i < array.Length; i++)
      {
        stringBuilder.Append(array[i].ToString("X2"));
      }
      return stringBuilder.ToString();
    }
  }
}