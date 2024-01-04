using Freelance.DAL.Abstract;
using Freelance.DAL.Concrete.EntityFramework;
using Freelance.Entities;
using Freelance.MvcWebUI.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Freelance.MvcWebUI.Controllers
{
    public class ProjectsController : Controller
    {
        IProjectDal _projectDal = new EFProjectDal();
        IOfferDal _offerDal = new EFOfferDal();
        IUserDal _userDal = new EFUserDal();
        IStateDal _stateDal = new EFStateDal();
        IPaymentDal _paymentDal = new EFPaymentDal();
        ProjectDetailViewModel projectDetailViewModel = new ProjectDetailViewModel();

        [Authorize]
        public ActionResult Index()
        {
            List<Project> projects = _projectDal.GetByHasNotWorker();
            return View(projects);
        }
        [Authorize]

        public ActionResult Details(int id)
        {
            Project project = _projectDal.GetProject(id);
            List<Offer> offers = _offerDal.GetOffersByProjectId(id);
            User owner = _userDal.GetUser(project.OwnerId);
          
            
            EFUserDal EFUserDal = new EFUserDal();

            projectDetailViewModel.Project = project;
            projectDetailViewModel.Offers = offers;
            projectDetailViewModel.Owner = owner;
            if (project.StateId != 1)
            {
                User worker = _userDal.GetUser((int)project.WorkerId);
                projectDetailViewModel.Worker = worker;
            }
            projectDetailViewModel.EFUserDal = EFUserDal;

            return View(projectDetailViewModel);
        }

        [Authorize]
        [HttpGet]
        public ActionResult Publish()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public ActionResult Publish(Project project)
        {
            project.OwnerId = Convert.ToInt32(Session["userID"]);
            project.ReleaseTime = DateTime.Now;
            project.StateId = new State().Id = 1 ;
            _projectDal.CreateProject(project);

            TempData.Add("message", String.Format("Proje başarılı bir şekilde yayımlandı."));

            return RedirectToAction("index", "Home");
        }


        [Authorize]
        [HttpPost]
        public ActionResult SetCompleteAsWorker(int projectId)
        {
            Project project = _projectDal.GetProject(projectId);
            project.StateId = 3;
            _projectDal.UpdateProject(project);

            TempData.Add("message", String.Format("Proje tamamlandı. Ödeme için proje sahibinden onay bekleniyor."));

            return RedirectToAction("index", "home");
        }

        [Authorize]
        [HttpPost]
        public ActionResult SetCompleteAsOwner(int projectId)
        {
            Payment payment = _paymentDal.FindByProjectId(projectId);
            Project project = _projectDal.GetProject(projectId);
            List<Offer> offers = _offerDal.GetOffersByProjectId(projectId);
            User owner = _userDal.GetUser(project.OwnerId);

            // Bütçe kontrolü
            if (payment.AcceptedPrice > owner.Credit)
            {
                if (TempData.ContainsKey("error"))
                {
                    TempData["message"] = String.Format("Hesap bakiyeniz, projenin bütçesinden düşük. Ödeme tamamlanamaz.");
                }
                else
                {
                    TempData.Add("message", String.Format("Hesap bakiyeniz, projenin bütçesinden düşük. Ödeme tamamlanamaz."));
                }
                return RedirectToAction("MyProjectsAsOwner", "Profile");
            }

            _paymentDal.MakePayment(payment);

            project.StateId = 4;
            _projectDal.UpdateProject(project);

            if (TempData.ContainsKey("message"))
            {
                TempData["message"] = String.Format("Proje onaylandı. Ödeme Tamamlandı.");
            }
            else
            {
                TempData.Add("message", String.Format("Proje onaylandı. Ödeme Tamamlandı."));
            }


            return RedirectToAction("index", "home");
        }

    }
}