using Autofac;
using HafizTakip.Business.Abstract;
using HafizTakip.Business.Concrete;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.DataAccess.Concrete.EntityFramework;
using HafizTakip.Core.Utilities.Security.Jwt;
using HafizTakip.Core.Utilities.Interceptors;
using Castle.DynamicProxy;
using Autofac.Extras.DynamicProxy;

namespace HafizTakip.Business.DependencyResolvers.Autofac;

public class AutofacBusinessModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<AuthService>().As<IAuthService>().InstancePerLifetimeScope();
        builder.RegisterType<EfUserDal>().As<IUserDal>().InstancePerLifetimeScope();
        builder.RegisterType<EfRefreshTokenDal>().As<IRefreshTokenDal>().InstancePerLifetimeScope();
        
        builder.RegisterType<JwtHelper>().As<ITokenHelper>().InstancePerLifetimeScope();

        builder.RegisterType<EfTenantDal>().As<ITenantDal>().InstancePerLifetimeScope();
        builder.RegisterType<TenantManager>().As<ITenantService>().InstancePerLifetimeScope();

        builder.RegisterType<EfGroupDal>().As<IGroupDal>().InstancePerLifetimeScope();
        builder.RegisterType<GroupManager>().As<IGroupService>().InstancePerLifetimeScope();

        builder.RegisterType<EfStudentDal>().As<IStudentDal>().InstancePerLifetimeScope();
        builder.RegisterType<StudentManager>().As<IStudentService>().InstancePerLifetimeScope();
        
        builder.RegisterType<UserManager>().As<IUserService>().InstancePerLifetimeScope();

        builder.RegisterType<EfLessonDal>().As<ILessonDal>().InstancePerLifetimeScope();
        builder.RegisterType<LessonManager>().As<ILessonService>().InstancePerLifetimeScope();

        builder.RegisterType<EfAttendanceDal>().As<IAttendanceDal>().InstancePerLifetimeScope();
        builder.RegisterType<AttendanceManager>().As<IAttendanceService>().InstancePerLifetimeScope();

        builder.RegisterType<EfRepetitionSessionDal>().As<IRepetitionSessionDal>().InstancePerLifetimeScope();

        builder.RegisterType<EfEtutNoteDal>().As<IEtutNoteDal>().InstancePerLifetimeScope();
        builder.RegisterType<EtutNoteManager>().As<IEtutNoteService>().InstancePerLifetimeScope();

        builder.RegisterType<EfExamDal>().As<IExamDal>().InstancePerLifetimeScope();
        builder.RegisterType<ExamManager>().As<IExamService>().InstancePerLifetimeScope();

        builder.RegisterType<EfExamResultDal>().As<IExamResultDal>().InstancePerLifetimeScope();
        builder.RegisterType<ExamResultManager>().As<IExamResultService>().InstancePerLifetimeScope();

        builder.RegisterType<EfAnnouncementDal>().As<IAnnouncementDal>().InstancePerLifetimeScope();
        builder.RegisterType<AnnouncementManager>().As<IAnnouncementService>().InstancePerLifetimeScope();

        builder.RegisterType<DashboardManager>().As<IDashboardService>().InstancePerLifetimeScope();

        builder.RegisterType<EfParentNotificationDal>().As<IParentNotificationDal>().InstancePerLifetimeScope();
        builder.RegisterType<ParentNotificationManager>().As<IParentNotificationService>().InstancePerLifetimeScope();

        builder.RegisterType<EfSubscriptionDal>().As<ISubscriptionDal>().InstancePerLifetimeScope();
        builder.RegisterType<SubscriptionManager>().As<ISubscriptionService>().InstancePerLifetimeScope();

        builder.RegisterType<EfInvoiceDal>().As<IInvoiceDal>().InstancePerLifetimeScope();
        builder.RegisterType<InvoiceManager>().As<IInvoiceService>().InstancePerLifetimeScope();

        builder.RegisterType<EfDailyAssignmentDal>().As<IDailyAssignmentDal>().InstancePerLifetimeScope();
        builder.RegisterType<DailyAssignmentManager>().As<IDailyAssignmentService>().InstancePerLifetimeScope();

        builder.RegisterType<EfStudentParentDal>().As<IStudentParentDal>().InstancePerLifetimeScope();
        builder.RegisterType<StudentParentManager>().As<IStudentParentService>().InstancePerLifetimeScope();

        // AOP Interceptor Kaydı
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();

        builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces()
            .EnableInterfaceInterceptors(new ProxyGenerationOptions()
            {
                Selector = new AspectInterceptorSelector()
            }).InstancePerLifetimeScope();
    }
}
