﻿using LPBugTracker.Helpers;
using LPBugTracker.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using PagedList;
using PagedList.Mvc;

namespace LPBugTracker.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private UserRolesHelper roleHelper = new UserRolesHelper();
        private ProjectHelper projHelper = new ProjectHelper();
        string demoAdminId = "26222e84-ebd1-4b2c-97cd-875773afd07f";
        public ActionResult Index()
        {
            return View();
        }
        
    
        public ActionResult AssignRole()
        {
            //I want to load up a Viewbag property that holds each of the users in my system.
            ViewBag.Users = new SelectList(db.Users, "Id", "Email");

            //I want to load up a Viewbag property that holds each of the roles in my system.
            ViewBag.Roles = new SelectList(db.Roles, "Name", "Name");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AssignRole(string users, string roles)
        {
            return RedirectToAction("Index", "Home");
        }

        public ActionResult Dashboard(int? page, string searchStr)
        {
            ViewBag.Search = searchStr;
            var projList = SearchHelper.IndexSearch(searchStr);

            int pageSize = 5;
            int pageNumber = (page ?? 1);
            var projects = db.Projects.OrderByDescending(p => p.Id).ToPagedList(pageNumber, pageSize);
            return View(projList.ToPagedList(pageNumber,pageSize));
        }

        

        public ActionResult UpdateUsers(int projectId)
        {
            var project = db.Projects.Find(projectId);

            var devs = roleHelper.UsersInRole("Developer");
            var assignedDevs = projHelper.GetProjectUsersInRole("Developer", projectId);
            var assignedDevIds = new List<string>();
            foreach (var user in assignedDevs)
            {
                assignedDevIds.Add(user.Id);
            }
            ViewBag.Developers = new MultiSelectList(devs, "Id", "FullName", assignedDevIds);

            var pms = roleHelper.UsersInRole("Project Manager");
            var assignedPM = projHelper.GetProjectUsersInRole("Project Manager", projectId).FirstOrDefault();
            if (assignedPM == null)
            {
                ViewBag.ProjectManagers = new SelectList(pms, "Id", "FullName");
            }
            else
            {
                ViewBag.ProjectManagers = new SelectList(pms, "Id", "FullName", assignedPM.Id);
            }
            

            var subs = roleHelper.UsersInRole("Submitter");
            var assignedSubs = projHelper.GetProjectUsersInRole("Submitter", projectId);
            var assignedSubIds = new List<string>();
            foreach (var user in assignedSubs)
            {
                assignedSubIds.Add(user.Id);
            }
            ViewBag.Submitters = new MultiSelectList(subs, "Id", "FullName", assignedSubIds);

            return View(project);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UpdateUsers(string ProjectManagers, List<string> Developers, List<string> Submitters, int projectId)
        {
            var project = db.Projects.Find(projectId);

            foreach (var user in project.Users)
            {
                await projHelper.RemoveUserFromProject(user.Id, projectId);
            }
            projHelper.AddUserToProject(ProjectManagers, projectId);
            foreach (var user in Developers)
            {
                projHelper.AddUserToProject(user, projectId);
            }
            foreach (var user in Submitters)
            {
                projHelper.AddUserToProject(user, projectId);
            }
            TempData["Message"] = "Project Users Updated Successfully.";
            return RedirectToAction("Dashboard");
        }

        public ActionResult FixProfilePictures()
        {
            var users = db.Users;
            foreach (var user in users)
            {
                if (roleHelper.IsUserInRole(user.Id, "Developer"))
                {
                    user.AvatarPath = "img/devprofilepic.png";
                    db.Users.Attach(user);
                    db.Entry(user).Property(u => u.AvatarPath).IsModified = true;
                }
                else if(roleHelper.IsUserInRole(user.Id, "Project Manager"))
                {
                    user.AvatarPath = "img/pmprofilepic.png";
                    db.Users.Attach(user);
                    db.Entry(user).Property(u => u.AvatarPath).IsModified = true;
                }
                else if (roleHelper.IsUserInRole(user.Id, "Submitter"))
                {
                    user.AvatarPath = "img/subprofilepic.png";
                    db.Users.Attach(user);
                    db.Entry(user).Property(u => u.AvatarPath).IsModified = true;
                }
                else if (roleHelper.IsUserInRole(user.Id, "Admin"))
                {
                    user.AvatarPath = "img/adminprofilepic.png";
                    db.Users.Attach(user);
                    db.Entry(user).Property(u => u.AvatarPath).IsModified = true;
                }
                else if (user.AvatarPath == null)
                {
                    user.AvatarPath = "img/defaultprofilepic.jpg";
                    db.Users.Attach(user);
                    db.Entry(user).Property(u => u.AvatarPath).IsModified = true;
                }
            }
            db.SaveChanges();
            TempData["Message"] = "Null Profile Images replaced with Default Image!";
            return RedirectToAction("Index", "Users");
        }

        public ActionResult CreateProject()
        {
            var pms = roleHelper.UsersInRole("Project Manager");
            ViewBag.ProjectManagers = new SelectList(pms, "Id", "FullName");
            var devs = roleHelper.UsersInRole("Developer");
            ViewBag.Developers = new MultiSelectList(devs, "Id", "FullName");
            var subs = roleHelper.UsersInRole("Submitter");
            ViewBag.Submitters = new MultiSelectList(subs, "Id", "FullName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateProject(Project project, string ProjectManagers, List<string> Developers, List<string> Submitters)
        {
            if (ModelState.IsValid)
            {
                db.Projects.Add(project);
                db.SaveChanges();

                foreach (var dev in Developers)
                {
                    projHelper.AddUserToProject(dev, project.Id);
                }
                foreach (var sub in Submitters)
                {
                    projHelper.AddUserToProject(sub, project.Id);
                }
                projHelper.AddUserToProject(ProjectManagers, project.Id);

                TempData["Message"] = "Project Created Successfully";
                return RedirectToAction("Dashboard");
            }

            return View(project);
        }

        public ActionResult ManageRoles()
        {
            var users = db.Users.ToList();
            if (User.Identity.GetUserId() == demoAdminId)
            {
                users = db.Users.Where(u => u.Email.Contains("SolutionCenterDemo")).ToList();
            }
            

            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ManageRoles(string userId, string roleName)
        {
            var user = db.Users.Find(userId);
            var currentRole = roleHelper.ListUserRoles(userId).FirstOrDefault();
            roleHelper.RemoveUserFromRole(userId, currentRole);
            roleHelper.AddUserToRole(userId, roleName);

            TempData["roleMessage"] = user.FullName + "'s role has been updated to " + roleName + ".";
            return RedirectToAction("ManageRoles");
        }
    }
}