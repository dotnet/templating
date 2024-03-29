﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace MyProject.StarterWeb.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

#if EnableContactPage
        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

#endif
        public IActionResult Error()
        {
            return View();
        }
    }
}
