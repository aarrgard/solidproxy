# solidproxy
The solid proxy project can be used to implement and/or wrap registrations in an IoC container. It enables developers to register interceptors or middlewares similar to the ones used in the .Net Core Http stack. 

    public class MethodConsoleLogger<TObject, TMethodType, TPipeline> 
       : ISolidProxyInvocationStep<TObject, TMethodType, TPipeline>
    {
        public async Task<TPipeline> Handle(
            Func<Task<TPipeline>> next, 
            ISolidProxyInvocation<TObject, TMethodType, TPipeline> invocation)
        {
            try {
                Console.WriteLine($"Entering {invocation.Configuration.MethodInfo.Name}");
                return await next();
            } finally {
                Console.WriteLine($"Exited {invocation.Configuration.MethodInfo.Name}");            
            }
        }
    }
 
