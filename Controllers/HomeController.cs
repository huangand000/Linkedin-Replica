using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using belt_retake.Models;
using belt_retake.Connection;
using Microsoft.AspNetCore.Http;

namespace belt_retake.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        [Route("")]
        public IActionResult LoginReg()
        {
            ViewBag.Errors = TempData["Errors"];
            return View("LoginReg");
        }
        [HttpGet]
        [Route("/dashboard")]
        public IActionResult Dashboard() {
            if(HttpContext.Session.GetInt32("user_id") == null) {
                return RedirectToAction("LoginReg");
            }
            ViewBag.name = HttpContext.Session.GetString("name");
            ViewBag.description = HttpContext.Session.GetString("description");
            string query1 = ($"SELECT invitation.id as InvitationId, users.name as myName, users.id as myId, users2.name as friendName, users2.id as friendId FROM invitation LEFT JOIN users ON users.id = user_id LEFT JOIN users as users2 ON users2.id = friend_id WHERE users2.id = {HttpContext.Session.GetInt32("user_id")};");
            ViewBag.allInvitations = DbConnector.Query(query1);
            string query2 = ($"SELECT network.id as NetworkId, users.name as myName, users.id as myId, users2.name as friendName, users2.id as friendId FROM network LEFT JOIN users ON users.id = user_id LEFT JOIN users as users2 ON users2.id = friend_id WHERE users2.id = {HttpContext.Session.GetInt32("user_id")} or users.id = {HttpContext.Session.GetInt32("user_id")};");
            ViewBag.allNetwork = DbConnector.Query(query2);
            ViewBag.name = HttpContext.Session.GetString("name");
            return View("Dashboard");
        }

        [HttpGet]
        [Route("/logout")]
        public IActionResult Logout() {
            HttpContext.Session.Clear();
            return RedirectToAction("LoginReg");
        }

        [HttpPost]
        [Route("register")]
        public IActionResult Register(User user) {
            string query0 = ($"SELECT * FROM users WHERE users.email='{user.Email}'");
            var unique = DbConnector.Query(query0).SingleOrDefault();
            if(unique != null) {
                ModelState.AddModelError("UserName", "Username taken");
                return View("LoginReg");
            }
            if(ModelState.IsValid) {
                string query = ($"INSERT INTO users (name, email, password, created_at, updated_at, description) VALUES ('{user.Name}', '{user.Email}', '{user.Password}', NOW(), NOW(), '{user.Description}')");
                DbConnector.Execute(query);
                HttpContext.Session.SetString("name", user.Name);
                string query2 =($"SELECT * FROM users WHERE email='{user.Email}'");
                Dictionary<string, object> currUser = DbConnector.Query(query2).SingleOrDefault();
                HttpContext.Session.SetInt32("user_id", (int) currUser["id"]);
                HttpContext.Session.SetString("name", user.Name);
                HttpContext.Session.SetString("description", user.Description);
                string query3 = ($"SELECT * from users WHERE users.id={HttpContext.Session.GetInt32("user_id")}");
                var currUser1 = DbConnector.Query(query2).SingleOrDefault();
                return RedirectToAction("Dashboard");
            } else {
                return View("LoginReg");
            }
        }
        
        [HttpPost]
        [Route("login")]
        public IActionResult Login(string Email, string Password) {
            string query = ($"SELECT * from users WHERE email='{Email}'");
            var user = DbConnector.Query(query).FirstOrDefault();
            if(user == null) {
                TempData["Errors"] = "Invalid Username/Password";
            } else {
                if((string) user["password"] != Password) {
                    TempData["Errors"] = "Email/Password Mismatch";
                } else {
                    HttpContext.Session.SetInt32("user_id", (int) user["id"]);
                    HttpContext.Session.SetString("name", (string) user["name"]);
                    HttpContext.Session.SetString("description", (string) user["description"]);
                    string query2 = ($"SELECT * from users WHERE users.id={HttpContext.Session.GetInt32("user_id")}");
                    var currUser = DbConnector.Query(query2).SingleOrDefault();
                    return RedirectToAction("Dashboard");
                }
            }
            return RedirectToAction("LoginReg");
        }

        [HttpGet]
        [Route("/users")]
        
        public IActionResult AllUsers() {
            if(HttpContext.Session.GetInt32("user_id") == null) {
                return RedirectToAction("LoginReg");
            }
            string query1 = ($"SELECT users.name as myName, users.id as myId, users2.name as friendName, users2.id as friendId FROM invitation LEFT JOIN users ON users.id = user_id LEFT JOIN users as users2 ON users2.id = friend_id WHERE users.id = {HttpContext.Session.GetInt32("user_id")} OR users2.id ={HttpContext.Session.GetInt32("user_id")}");
            ViewBag.allInvitations = DbConnector.Query(query1);
            string query3 = ($"SELECT users.name as myName, users.id as myId, users2.name as friendName, users2.id as friendId FROM network LEFT JOIN users ON users.id = user_id LEFT JOIN users as users2 ON users2.id = friend_id where users.id = {HttpContext.Session.GetInt32("user_id")} OR users2.id ={HttpContext.Session.GetInt32("user_id")};");
            ViewBag.allNetwork = DbConnector.Query(query3);
            string query = ($"SELECT * from users WHERE users.id !={HttpContext.Session.GetInt32("user_id")}");
            ViewBag.otherUsers = DbConnector.Query(query);
            
            Dictionary<object,int> dict = new Dictionary<object, int>();
            foreach(var user in ViewBag.allInvitations) {
                dict[user["myName"]] = 1;
                dict[user["friendName"]] = 1;
                
            }
            foreach(var user in ViewBag.allNetwork) {
                dict[user["myName"]] = 1;
                dict[user["friendName"]] = 1;

            }

            List<Dictionary<string,object>> myList = new List<Dictionary<string, object>>();
            foreach(var user in ViewBag.otherUsers) {
                if (!dict.ContainsKey(user["name"])) {
                    Dictionary<string,object> curr = new Dictionary<string,object>();
                    curr.Add("name", user["name"]);
                    curr.Add("user_id", user["id"]);
                    myList.Add(curr);
                    curr = new Dictionary<string,object>();
                }
            }

            ViewBag.displayUsers = myList;
            return View("AllUsers");
        }

        [HttpPost]
        [Route("/connectUser")]
        public IActionResult ConnectUser(int id) {
            if(HttpContext.Session.GetInt32("user_id") == null) {
                return RedirectToAction("LoginReg");
            }
            System.Console.WriteLine(id);
            string query = ($"INSERT INTO invitation (user_id, friend_id, created_at, updated_at) VALUES ({HttpContext.Session.GetInt32("user_id")}, {id}, NOW(), NOW())");
            DbConnector.Execute(query);
            return RedirectToAction("AllUsers");
        }
        
        [HttpPost]
        [Route("/accept")]
        
        public IActionResult AddNetwork(int id, int invitationId) {
            if(HttpContext.Session.GetInt32("user_id") == null) {
                return RedirectToAction("LoginReg");
            }
            string query = ($"INSERT INTO network (user_id, friend_id, created_at, updated_at) VALUES ({HttpContext.Session.GetInt32("user_id")}, {id}, NOW(), NOW())");
            DbConnector.Execute(query);
            string query2 = ($"DELETE FROM invitation WHERE id={invitationId}");
            DbConnector.Execute(query2);
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [Route("/ignore")]
        public IActionResult IgnoreNetwork(int id) {
            if(HttpContext.Session.GetInt32("user_id") == null) {
                return RedirectToAction("LoginReg");
            }
            string query = ($"DELETE FROM invitation WHERE id={id}");
            DbConnector.Execute(query);
            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        [Route("/users/{id}")]
        public IActionResult SpecUser(int id) {
            if(HttpContext.Session.GetInt32("user_id") == null) {
                return RedirectToAction("LoginReg");
            }
            string query = ($"SELECT * from users WHERE users.id={id}");
            ViewBag.SpecUser = DbConnector.Query(query).SingleOrDefault();
            return View("SpecUser");
        }

        [HttpGet]
        [Route("/myProfile")]
        public IActionResult MyProfile() {
            if(HttpContext.Session.GetInt32("user_id") == null) {
                return RedirectToAction("LoginReg");
            }
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [Route("/deleteNetwork")]
        public IActionResult DeleteNetwork(int id) {
            if(HttpContext.Session.GetInt32("user_id") == null) {
                return RedirectToAction("LoginReg");
            }
            string query = ($"DELETE FROM network WHERE id={id}");
            DbConnector.Execute(query);
            return RedirectToAction("Dashboard");
        }

       
    }
}
