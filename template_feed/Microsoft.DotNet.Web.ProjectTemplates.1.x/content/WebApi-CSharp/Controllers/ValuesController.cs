﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#if (OrganizationalAuth || WindowsAuth)
using Microsoft.AspNetCore.Authorization;
#endif
using Microsoft.AspNetCore.Mvc;

namespace Company.WebApplication1.Controllers
{
#if (OrganizationalAuth || WindowsAuth)
    [Authorize]
#endif
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        // GET api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        [HttpGet("{id:int}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
#if (OrganizationalAuth || WindowsAuth)
            // For more information on protecting this API from Cross Site Request Forgery (CSRF) attacks, see https://go.microsoft.com/fwlink/?LinkID=717803
#endif
        }

        // PUT api/values/5
        [HttpPut("{id:int}")]
        public void Put(int id, [FromBody]string value)
        {
#if (OrganizationalAuth || WindowsAuth)
            // For more information on protecting this API from Cross Site Request Forgery (CSRF) attacks, see https://go.microsoft.com/fwlink/?LinkID=717803
#endif
        }

        // DELETE api/values/5
        [HttpDelete("{id:int}")]
        public void Delete(int id)
        {
#if (OrganizationalAuth || WindowsAuth)
            // For more information on protecting this API from Cross Site Request Forgery (CSRF) attacks, see https://go.microsoft.com/fwlink/?LinkID=717803
#endif
        }
    }
}
