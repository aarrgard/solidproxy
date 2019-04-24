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
 
Have a look at the [wiki](https://github.com/aarrgard/solidproxy/wiki) for more info.

The SolidProxy is used by the [SolidRpc](https://github.com/aarrgard/solidrpc/wiki) project to setup and configure rpc calls.
