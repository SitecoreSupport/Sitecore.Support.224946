using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Forms.Mvc;
using Sitecore.Forms.Mvc.Attributes;
using Sitecore.Forms.Mvc.Controllers;
using Sitecore.Forms.Mvc.Controllers.Filters;
using Sitecore.Forms.Mvc.Controllers.ModelBinders;
using Sitecore.Forms.Mvc.Interfaces;
using Sitecore.Forms.Mvc.Models;
using Sitecore.Forms.Mvc.ViewModels;
using Sitecore.Mvc.Controllers;
using Sitecore.WFFM.Abstractions.Dependencies;
using Sitecore.WFFM.Abstractions.Shared;
using System.IO;
using System.Web.Mvc;

namespace Sitecore.Forms.Mvc.Controllers
{
  [ModelBinder(typeof(FormModelBinder))]
  public class FormController : SitecoreController
  {
    private readonly IAnalyticsTracker analyticsTracker;

    public IRepository<FormModel> FormRepository
    {
      get;
      private set;
    }

    public IAutoMapper<IFormModel, FormViewModel> Mapper
    {
      get;
      private set;
    }

    public IFormProcessor<FormModel> FormProcessor
    {
      get;
      private set;
    }

    public FormController()
        : this((IRepository<FormModel>)Factory.CreateObject(Constants.FormRepository, true), (IAutoMapper<IFormModel, FormViewModel>)Factory.CreateObject(Constants.FormAutoMapper, true), (IFormProcessor<FormModel>)Factory.CreateObject(Constants.FormProcessor, true), DependenciesManager.AnalyticsTracker)
    {
    }

    public FormController(IRepository<FormModel> repository, IAutoMapper<IFormModel, FormViewModel> mapper, IFormProcessor<FormModel> processor, IAnalyticsTracker analyticsTracker)
    {
      Assert.ArgumentNotNull(repository, "repository");
      Assert.ArgumentNotNull(mapper, "mapper");
      Assert.ArgumentNotNull(processor, "processor");
      Assert.ArgumentNotNull(analyticsTracker, "analyticsTracker");
      FormRepository = repository;
      Mapper = mapper;
      FormProcessor = processor;
      this.analyticsTracker = analyticsTracker;
    }

    [FormErrorHandler]
    [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Head)]
    public override ActionResult Index()
    {
      return Form();
    }

    [SubmittedFormHandler]
    [FormErrorHandler]
    [HttpPost]
    [WffmLimitMultipleSubmits]
    [WffmValidateAntiForgeryToken]
    public virtual ActionResult Index([ModelBinder(typeof(FormModelBinder))] FormViewModel formViewModel)
    {
      analyticsTracker.InitializeTracker();
      return ProcessedForm(formViewModel, "");
    }

    [FormErrorHandler]
    [AllowCrossSiteJson]
    public virtual JsonResult Process([ModelBinder(typeof(FormModelBinder))] FormViewModel formViewModel)
    {
      analyticsTracker.InitializeTracker();
      ProcessedFormResult<FormModel, FormViewModel> processedFormResult = ProcessedForm(formViewModel, "~/Views/Form/Index.cshtml");
      processedFormResult.ExecuteResult(base.ControllerContext);
      string data = default(string);
      using (StringWriter stringWriter = new StringWriter())
      {
        ViewContext viewContext = new ViewContext(base.ControllerContext, processedFormResult.View, base.ViewData, base.TempData, stringWriter);
        processedFormResult.View.Render(viewContext, stringWriter);
        data = stringWriter.GetStringBuilder().ToString();
      }
      base.ControllerContext.HttpContext.Response.Clear();
      return new JsonResult
      {
        Data = data
      };
    }

    public virtual FormResult<FormModel, FormViewModel> Form()
    {
      return new FormResult<FormModel, FormViewModel>(FormRepository, Mapper)
      {
        ViewData = base.ViewData,
        TempData = base.TempData,
        ViewEngineCollection = base.ViewEngineCollection
      };
    }

    public virtual ProcessedFormResult<FormModel, FormViewModel> ProcessedForm(FormViewModel viewModel, string viewName = "")
    {
      ProcessedFormResult<FormModel, FormViewModel> processedFormResult = new ProcessedFormResult<FormModel, FormViewModel>(FormRepository, Mapper, FormProcessor, viewModel)
      {
        ViewData = base.ViewData,
        TempData = base.TempData,
        ViewEngineCollection = base.ViewEngineCollection
      };
      if (!string.IsNullOrEmpty(viewName))
      {
        processedFormResult.ViewName = viewName;
      }
      return processedFormResult;
    }
  }
}