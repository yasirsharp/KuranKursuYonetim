using Castle.DynamicProxy;
using FluentValidation;
using HafizTakip.Core.Utilities.Interceptors;
using HafizTakip.Core.CrossCuttingConcerns.Validation;

namespace HafizTakip.Core.Aspects.Autofac.Validation;

public class ValidationAspect : MethodInterception
{
    private Type _validatorType;
    public ValidationAspect(Type validatorType)
    {
        if (!typeof(IValidator).IsAssignableFrom(validatorType))
        {
            throw new Exception("Bu bir doğrulama sınıfı değil.");
        }
        _validatorType = validatorType;
    }
    
    protected override void OnBefore(IInvocation invocation)
    {
        var validator = (IValidator)Activator.CreateInstance(_validatorType)!;
        var entityType = _validatorType.BaseType?.GetGenericArguments()[0];
        if (entityType == null) return;
        
        var entities = invocation.Arguments.Where(t => t.GetType() == entityType);
        foreach (var entity in entities)
        {
            ValidationTool.Validate(validator, entity);
        }
    }
}
