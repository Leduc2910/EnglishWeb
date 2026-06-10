using Microsoft.AspNetCore.Mvc;

namespace EnglishTestWeb.Api.Application.Security;

public interface IHiddenResourceResponseFactory
{
    ActionResult FromDecision(AuthorizationDecision decision);

    ActionResult FromCode(int statusCode, string code, string title, string detail);
}
