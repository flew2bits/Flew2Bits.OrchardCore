using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flew2Bits.Media.AWS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OrchardCore.Modules;

namespace Flew2Bits.Media.AWS.Controllers
{
    [Feature("Flew2Bits.Media.AWS.Storage")]
    public class AdminController : Controller
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly AWSS3StorageOptions _options;

        public AdminController(
            IAuthorizationService authorizationService,
            IOptions<AWSS3StorageOptions> options)
        {
            _authorizationService = authorizationService;
            _options = options.Value;
        }
        public async Task<IActionResult> Options()
        {
            if (!await _authorizationService.AuthorizeAsync(User, Permissions.ViewAWSMediaOptions))
            {
                return Forbid();
            }

            var model = new OptionsViewModel
            {
                BucketName = _options.BucketName,
                Secret = _options.Secret,
                Key = _options.Key,
                EndPoint = _options.EndPoint,
                BasePath = _options.BasePath
            };

            return View(model);
        }
    }
}
