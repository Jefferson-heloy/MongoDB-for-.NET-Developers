using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using MongoDB.Driver;
using M101DotNet.WebApp.Models;
using M101DotNet.WebApp.Models.Home;
using MongoDB.Bson;
using System.Linq.Expressions;

namespace M101DotNet.WebApp.Controllers
{
    public class HomeController : Controller
    {
        public async Task<ActionResult> Index()
        {
            var blogContext = new BlogContext();

            // find the most recent 10 posts and order them
            // from newest to oldest
            var recentPosts = await blogContext.Posts.Find(_ => true).SortByDescending(x => x.CreatedAtUtc)
                                                                     .Limit(10)
                                                                     .ToListAsync();

            var model = new IndexModel
            {
                RecentPosts = recentPosts
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult NewPost()
        {
            return View(new NewPostModel());
        }

        [HttpPost]
        public async Task<ActionResult> NewPost(NewPostModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var blogContext = new BlogContext();
            // Insert the post into the posts collection
            var post = new Post
            {
                Author = User.Identity.Name,
                Title = model.Title,
                Content = model.Content,
                Tags = model.Tags.Split(',').ToList(),
                CreatedAtUtc = DateTime.UtcNow,
                Comments = new List<Comment>()
            };

            await blogContext.Posts.InsertOneAsync(post);

            return RedirectToAction("Post", new { id = post.Id });
        }

        [HttpGet]
        public async Task<ActionResult> Post(string id)
        {
            var blogContext = new BlogContext();

            // Find the post with the given identifier
            var post = await blogContext.Posts.Find(x => x.Id == id).FirstOrDefaultAsync();

            if (post == null)
            {
                return RedirectToAction("Index");
            }

            var model = new PostModel
            {
                Post = post
            };

            return View(model);
        }

        [HttpGet]
        public async Task<ActionResult> Posts(string tag = null)
        {
            var blogContext = new BlogContext();

            // Find all the posts with the given tag if it exists.
            // Otherwise, return all the posts.
            // Each of these results should be in descending order.
            List<Post> posts;
            if (string.IsNullOrEmpty(tag))
            {
                posts = await blogContext.Posts.Find(_ => true)
                                               .SortByDescending(x => x.CreatedAtUtc)
                                               .ToListAsync();
            }
            else
            {
                posts = await blogContext.Posts.Find(Builders<Post>.Filter.AnyEq(post => post.Tags, tag))
                                               .SortByDescending(x => x.CreatedAtUtc)
                                               .ToListAsync();
            }

            return View(posts);
        }

        [HttpPost]
        public async Task<ActionResult> NewComment(NewCommentModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Post", new { id = model.PostId });
            }

            var blogContext = new BlogContext();

            // add a comment to the post identified by model.PostId.
            // you can get the author from "this.User.Identity.Name"
            var comment = new Comment
            {
                Author = User.Identity.Name,
                Content = model.Content,
                CreatedAtUtc = DateTime.UtcNow
            };

            var filter = Builders<Post>.Filter.Eq(post => post.Id, model.PostId);
            var update = Builders<Post>.Update.Push(post => post.Comments, comment);

            await blogContext.Posts.UpdateOneAsync(filter, update);

            return RedirectToAction("Post", new { id = model.PostId });
        }
    }
}