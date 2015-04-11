using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NewLife.Cube.Areas.Admin.Controllers
{
    public class SysController : Controller
    {
        //
        // GET: /Admin/Sys/

        public ActionResult Index()
        {
            return View();
        }

        //
        // GET: /Admin/Sys/Details/5

        public ActionResult Details(int id)
        {
            return View();
        }

        //
        // GET: /Admin/Sys/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /Admin/Sys/Create

        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        //
        // GET: /Admin/Sys/Edit/5

        public ActionResult Edit(int id)
        {
            return View();
        }

        //
        // POST: /Admin/Sys/Edit/5

        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        //
        // GET: /Admin/Sys/Delete/5

        public ActionResult Delete(int id)
        {
            return View();
        }

        //
        // POST: /Admin/Sys/Delete/5

        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
