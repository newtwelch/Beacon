using Backend.API.Wrapper;
using Backend.Core.Models.Songs;
using Backend.Core.Services;
using Microsoft.AspNetCore.Mvc;
using SQLite;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Backend.API.Controllers
{
    [Route("songs")]
    [ApiController]
    public class SongsController : ControllerBase
    {
        private readonly SongDbConnection dbConnection;
        private readonly ILogger<SongsController> logger;

        public SongsController(SongDbConnection _dbConnection, ILogger<SongsController> _logger)
        {
            dbConnection = _dbConnection;
            logger = _logger;
            dbConnection.CreateTableAsync<Song>().Wait();
        }

        // GET: api/<SongsController>
        [HttpGet]
        public async Task<List<Song>> Get()
        {
            return await dbConnection.Table<Song>().ToListAsync();
        }

        [HttpGet("stream")]
        public async IAsyncEnumerable<Song> Stream()
        {
            var query = dbConnection.Table<Song>().Take(10);
            var songs = await query.ToListAsync();

            foreach (var song in songs)
            {
                yield return song;
            }
        }


        // GET api/<SongsController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<SongsController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<SongsController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<SongsController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
