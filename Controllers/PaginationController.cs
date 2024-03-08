using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace WebService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaginationController : ControllerBase
    {

        //Database Integration

        public class ApplicationDbContext : DbContext
        {
            public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
                : base(options)
            {
            }


            //Database Tables
            public DbSet<EpisodeResult> Episodes { get; set; }
            public DbSet<CharacterResult> Characters { get; set; }

            public DbSet<FavoriteCharacter> FavoriteCharacters { get; set; }


          
        }

        private readonly ApplicationDbContext _context;

        public PaginationController(ApplicationDbContext context)
        {
            _context = context;
        }


        //Api url's to retrive data.

        private const string apiUrl = "https://rickandmortyapi.com/api/episode";
        private const string characterApiUrl = "https://rickandmortyapi.com/api/character";




        //This request gets data of all episodes to list in main page.

        [HttpGet("all")]
        public async Task<IActionResult> GetAllEpisodesDetails()
        {
            try
            {
                var allEpisodes = await GetEpisodesFromApi(apiUrl);

                if (allEpisodes != null && allEpisodes.results.Length > 0)
                {
                    return Ok(allEpisodes.results);
                }
                else
                {
                    return NotFound("No episodes found.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

       

        //search request that brings any character or episode to the page
        //for example if rick searched character or episode card's show up.
        [HttpGet("search")]
        public async Task<IActionResult> SearchEpisodes(string search = "")
        {
            try
            {
                var episodes = await GetEpisodesFromApi($"{apiUrl}?name={search}");

                if (episodes != null && episodes.results.Length > 0)
                {
                    return Ok(episodes.results);
                }
                else
                {
                    return NotFound($"No episodes found with search term '{search}'.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }


        //when a episode card clicked details of episode pop up.

        [HttpGet("{episodeId}/details")]
        public async Task<IActionResult> GetEpisodeDetails(int episodeId)
        {
            try
            {
                var episode = await GetEpisodeDetailsFromApi($"{apiUrl}/{episodeId}");

                if (episode != null)
                {
                    return Ok(episode);
                }
                else
                {
                    return NotFound($"Episode with id {episodeId} not found.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        //based on episode selected character details will be demonstrated

        [HttpGet("{episodeId}/characters")]
        public async Task<IActionResult> GetCharactersInEpisode(int episodeId)
        {
            try
            {
                var characters = await GetCharactersFromEpisode($"{apiUrl}/{episodeId}");

                if (characters != null && characters.Length > 0)
                {
                    return Ok(characters);
                }
                else
                {
                    return NotFound($"No characters found for episode with id {episodeId}.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }


        //To retrive episodes from api this method used. Inspects Json result and saves the episode object.
        private async Task<EpisodeResponse> GetEpisodesFromApi(string apiUrl)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<EpisodeResponse>();
                }
                else
                {
                    return null;
                }
            }
        }

        // inspects retrvied episode object and get its details 
        private async Task<EpisodeResult> GetEpisodeDetailsFromApi(string apiUrl)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<EpisodeResult>();
                }
                else
                {
                    return null;
                }
            }
        }


        //retrives character info from episode s
        private async Task<CharacterResult[]> GetCharactersFromEpisode(string apiUrl)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    var episode = await response.Content.ReadFromJsonAsync<EpisodeResult>();
                    if (episode != null && episode.characters.Length > 0)
                    {
                      
                        List<CharacterResult> characters = new List<CharacterResult>();
                        foreach (var characterUrl in episode.characters)
                        {
                            var character = await client.GetFromJsonAsync<CharacterResult>(characterUrl);
                            if (character != null)
                            {
                                characters.Add(character);  //this loop inspects character array to spot unique characters 
                                //and saves each character.
                            }
                        }
                        return characters.ToArray();
                    }
                }
                return null;
            }
        }


        //this method will add a character to database by its id and also maximum 10 characters can be added.
        [HttpPost("favorites")]
        public IActionResult AddFavoriteCharacter([FromBody] int characterId)
        {
            try
            {
                // Check if the maximum limit of favorite characters is reached
                if (_context.FavoriteCharacters.Count() >= 10)
                {
                    return BadRequest("Maximum limit of 10 favorite characters reached.");
                }

                // Check if the character already exists in favorites
                var existingFavorite = _context.FavoriteCharacters
                    .FirstOrDefault(fc => fc.CharacterId == characterId);

                if (existingFavorite != null)
                {
                    return BadRequest("Character is already in favorites.");
                }

                // Add the character to favorites
                var favoriteCharacter = new FavoriteCharacter
                {
                    CharacterId = characterId
                };

                _context.FavoriteCharacters.Add(favoriteCharacter);
                _context.SaveChanges();

                return Ok("Character added to favorites.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }


        //to list all favorite characters
        [HttpGet("favorites")]
        public IActionResult GetFavoriteCharacters()
        {
            var favoriteCharacters = _context.FavoriteCharacters
                .Include(fc => fc.Character)
                .ToList();

            return Ok(favoriteCharacters);
        }


        //this is a method to remove a favorite character from favorite characters since there is a limit to store maximum 10 characters
        //user will need to remove a character from he or shes list to add new.
        [HttpDelete("favorites/{favoriteCharacterId}")]
        public IActionResult RemoveFavoriteCharacter(int favoriteCharacterId)
        {
            var favoriteCharacter = _context.FavoriteCharacters.Find(favoriteCharacterId);

            if (favoriteCharacter != null)
            {
                _context.FavoriteCharacters.Remove(favoriteCharacter);
                _context.SaveChanges();
                return Ok("Character removed from favorites.");
            }
            else
            {
                return NotFound("Favorite character not found.");
            }
        }


    }

    //episode object stored in here.
    public class EpisodeResponse
    {
        public EpisodeResult[] results { get; set; }
    }

    //episode details stored here.
    public class EpisodeResult
    {
        public int id { get; set; }
        public string name { get; set; }
        public string air_date { get; set; }
        public string episode { get; set; }
        public string[] characters { get; set; }
    }


    //character details.
    public class CharacterResult
    {
        public int id { get; set; }
        public string name { get; set; }
        public string status { get; set; }
        public string species { get; set; }
        public string type { get; set; }
        public string gender { get; set; }
        public Origin origin { get; set; }
        public Location location { get; set; }
        public string image { get; set; }
        public string[] episode { get; set; }
        public string url { get; set; }
        public DateTime created { get; set; }
    }


    //Origin prop in character
    public class Origin
    {
        public string name { get; set; }
        public string url { get; set; }
    }

    //Location prop in character
    public class Location
    {
        public string name { get; set; }
        public string url { get; set; }
    }

    //to store favorite characters
    public class FavoriteCharacter
    {
        public int Id { get; set; }
        public int CharacterId { get; set; }

        //related with character class.   
        public CharacterResult Character { get; set; }
    }
}
