using Microsoft.AspNetCore.Mvc;
using Nebula.Data;
using Nebula.Store.NodeViewUseCase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Nebula.Data.Lyra.Supply;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Nebula
{
    [Route("api/[controller]")]
    [ApiController]
    public class NebulaController : ControllerBase
    {
        public NebulaController()
        {

        }
        //// GET: api/<TokenController>
        //[HttpGet]
        //[Route("history")]
        //public IEnumerable<NodeViewState> GetNodesHistory()
        //{
        //    return History.FindAll();
        //}

        //[HttpGet]
        //[Route("supply")]
        //public SupplyInfo GetCurrentSupplyInfo()
        //{
        //    return History.GetCurrentSupply();
        //}

        //// GET api/<TokenController>/5
        //[HttpGet("{id}")]
        //public string Get(int id)
        //{
        //    return "value";
        //}

        //// POST api/<TokenController>
        //[HttpPost]
        //public void Post([FromBody] string value)
        //{
        //}

        //// PUT api/<TokenController>/5
        //[HttpPut("{id}")]
        //public void Put(int id, [FromBody] string value)
        //{
        //}

        //// DELETE api/<TokenController>/5
        //[HttpDelete("{id}")]
        //public void Delete(int id)
        //{
        //}
    }
}
