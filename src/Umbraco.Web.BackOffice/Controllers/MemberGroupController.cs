using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.Attributes;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Extensions;
using Constants = Umbraco.Cms.Core.Constants;

namespace Umbraco.Cms.Web.BackOffice.Controllers
{
    /// <summary>
    /// An API controller used for dealing with member groups
    /// </summary>
    [PluginController(Constants.Web.Mvc.BackOfficeApiArea)]
    [Authorize(Policy = AuthorizationPolicies.TreeAccessMemberGroups)]
    [ParameterSwapControllerActionSelector(nameof(GetById), "id", typeof(int), typeof(Guid), typeof(Udi))]
    public class MemberGroupController : UmbracoAuthorizedJsonController
    {
        private readonly IMemberGroupService _memberGroupService;
        private readonly UmbracoMapper _umbracoMapper;
        private readonly ILocalizedTextService _localizedTextService;
        private readonly RoleManager<IdentityRole> _roleManager;
     
        public MemberGroupController(
            IMemberGroupService memberGroupService,
            UmbracoMapper umbracoMapper,
            ILocalizedTextService localizedTextService,
            RoleManager<IdentityRole> roleManager
            )
        {
            _memberGroupService = memberGroupService ?? throw new ArgumentNullException(nameof(memberGroupService));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _umbracoMapper = umbracoMapper ?? throw new ArgumentNullException(nameof(umbracoMapper));
            _localizedTextService = localizedTextService ?? throw new ArgumentNullException(nameof(localizedTextService));
        }

        /// <summary>
        /// Gets the member group json for the member group id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ActionResult<MemberGroupDisplay>> GetById(int id)
        {
            //TODO: did we envisage this - combination of service and identity manager?
            IdentityRole identityRole = await _roleManager.FindByIdAsync(id.ToString());
            if (identityRole == null)
            {
                return NotFound();
            }

            IMemberGroup memberGroup = _memberGroupService.GetById(id);
            if (memberGroup == null)
            {
                return NotFound();
            }

            //TODO: the default identity role doesn't have all the properties IMemberGroup had, e.g. CreatorId
            MemberGroupDisplay dto = _umbracoMapper.Map<IMemberGroup, MemberGroupDisplay>(memberGroup);
            return dto;
        }


        /// <summary>
        /// Gets the member group json for the member group guid
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ActionResult<MemberGroupDisplay>> GetById(Guid id)
        {
            //TODO: did we envisage just identity or a combination of service and identity manager?
            IdentityRole identityRole = await _roleManager.FindByIdAsync(id.ToString());
            if (identityRole == null)
            {
                return NotFound();
            }
            //IMemberGroup memberGroup = _memberGroupService.GetById(id);
            //if (memberGroup == null)
            //{
            //    return NotFound();
            //}

            return _umbracoMapper.Map<IdentityRole, MemberGroupDisplay>(identityRole);
        }

        /// <summary>
        /// Gets the member group json for the member group udi
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ActionResult<MemberGroupDisplay>> GetById(Udi id)
        {
            var guidUdi = id as GuidUdi;
            if (guidUdi == null)
            {
                return NotFound();
            }

            //TODO: can we do this via identity?
            IdentityRole identityRole = await _roleManager.FindByIdAsync(id.ToString());
            if (identityRole == null)
            {
                return NotFound();
            }

            return _umbracoMapper.Map<IdentityRole, MemberGroupDisplay>(identityRole);
        }

        public async Task<IEnumerable<MemberGroupDisplay>> GetByIds([FromQuery]int[] ids)
        {
            var roles = new List<IdentityRole>();

            foreach (int id in ids)
            {
                IdentityRole role = await _roleManager.FindByIdAsync(id.ToString());
                roles.Add(role);
            }

            return roles.Select(x=> _umbracoMapper.Map<IdentityRole, MemberGroupDisplay>(x));
        }

        [HttpDelete]
        [HttpPost]
        public async Task<IActionResult> DeleteById(int id)
        {
            //TODO: are there any repercussions elsewhere for us changing these to async?
            IdentityRole role = await _roleManager.FindByIdAsync(id.ToString());

            if (role == null)
            {
                return NotFound();
            }

            IdentityResult roleDeleted = await _roleManager.DeleteAsync(role);
            if (roleDeleted.Succeeded)
            {
                return Ok();
            }
            else
            {
                return Problem("Issue during deletion - please see logs");
            }
        }

        public IEnumerable<MemberGroupDisplay> GetAllGroups() => _roleManager.Roles.Select(x => _umbracoMapper.Map<IdentityRole, MemberGroupDisplay>(x));

        public MemberGroupDisplay GetEmpty()
        {
            var item = new MemberGroup();
            return _umbracoMapper.Map<IMemberGroup, MemberGroupDisplay>(item);
        }

        public async Task<ActionResult<MemberGroupDisplay>> PostSave(MemberGroupSave saveModel)
        {
            int id = int.Parse(saveModel.Id.ToString());

            IdentityRole role = id > 0 ? await _roleManager.FindByIdAsync(saveModel.Id.ToString()) : null;

            if (role == null)
            {
                return NotFound();
            }

            role.Name = saveModel.Name;
            IdentityResult updatedResult = await _roleManager.UpdateAsync(role);

            if (!updatedResult.Succeeded)
            {
                //TODO: what to retrun if there is a failed identity result
                return Problem();
            }

            //TODO: should we return the identity role or return the group from the service?
            MemberGroupDisplay display = _umbracoMapper.Map<IdentityRole, MemberGroupDisplay>(role);

            display.AddSuccessNotification(
                            _localizedTextService.Localize("speechBubbles/memberGroupSavedHeader"),
                            string.Empty);

            return display;
        }
    }
}
