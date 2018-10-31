using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

using M101DotNet.WebApp.Models;
using M101DotNet.WebApp.Models.Home;
using System.Linq.Expressions;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Web.Mvc;

namespace M101DotNet.WebApp.Controllers
{
    public class HomeController : Controller
    {
        public async Task<ActionResult> Index()
        {
            var blogContext = new BlogContext();
            // XXX WORK HERE
            // find the most recent 10 posts and order them
            // from newest to oldest
            var filter = new BsonDocument();
            var recentPosts = await blogContext.Posts.Find(filter).Limit(10).Sort("{Date:1}").ToListAsync();

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
            // XXX WORK HERE
            // Insert the post into the posts collection
            var post = new Post
            {
                CreatedAtUtc = DateTime.UtcNow,
                Title = model.Title,
                Content = model.Content,
                Author = User.Identity.Name
            };
            string[] words = model.Tags.Split(',');
            post.Tags = new List<string>();
            foreach(string word in words)
            {
                post.Tags.Add(word);
            }

            await blogContext.Posts.InsertOneAsync(post);

            return RedirectToAction("Post", new { id = post.Id });
        }

        [HttpGet]
        public async Task<ActionResult> Post(string id)
        {
            var blogContext = new BlogContext();

            // XXX WORK HERE
            // Find the post with the given identifier
            var post = await blogContext.Posts.Find(x => x.Id == ObjectId.Parse(id)).SingleAsync();
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

            // XXX WORK HERE
            // Find all the posts with the given tag if it exists.
            // Otherwise, return all the posts.
            // Each of these results should be in descending order.
            var filter = new BsonDocument("Tags", tag);
            var posts = await blogContext.Posts.Find(filter).Limit(10).ToListAsync();
            return View(posts);
        }

        [HttpPost]
        public async Task<ActionResult> Comments(NewCommentModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Post", new { id = model.PostId });
            }

            var blogContext = new BlogContext();
            // XXX WORK HERE
            // add a comment to the post identified by model.PostId.
            // you can get the author from "this.User.Identity.Name"

            var tempModel = new Post();
            var tepCommentList = new List<Comment>();
            var comment = new Comment
            {
                CreatedAtUtc = DateTime.UtcNow,
                Author = User.Identity.Name,
                Content = model.Content
            };
            tepCommentList.Add(comment);

            tempModel = await blogContext.Posts.Find(x => x.Id == ObjectId.Parse(model.PostId)).SingleAsync();

            if (tempModel.Comments != null)
            {
                tempModel.Comments.Add(comment);
            }
            else
            {
                tempModel.Comments = tepCommentList;
            }
            var result = await blogContext.Posts.ReplaceOneAsync(x => x.Id == ObjectId.Parse(model.PostId), tempModel);


            return RedirectToAction("Post", new { id = model.PostId });
        }
    }
}