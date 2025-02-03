
## Create Provider (implementing from IDependencyProvider) use [Provide]

```c#
public class Provider : MonoBehaviour, IDependencyProvider
{
    [Provide] //provider itself
    public Provider provider(){
        return this
    } 
    [Provide]
    public ServiceA ProvideA()
    {
        return new ServiceA();
    }
    [Provide]
    public ServiceB ProvideB()
    {
        return new ServiceB();
    }
}
```

## Inject class in filed or method (use [Inject])

```c#
public class ClassA : MonoBehaviour
{
	[Inject] ServiceA serviceA;
    
    ServiceB serviceB;
    
    [Inject]
    void Init(ServiceB serviceB){
        this.serviceB = serviceB;
    }
}
```