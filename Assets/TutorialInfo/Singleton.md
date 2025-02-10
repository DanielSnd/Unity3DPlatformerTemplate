# Singleton<T>

This template comes with a Singleton<T> class that simplifies the process of creating a singleton.

![URP](Icons/URP.png)
## Creating Your Own Singleton

To create a new manager using the Singleton<T> class:

```csharp
public class MyManager : Singleton<MyManager>
{
        // If you need to do anything special when the singleton is created,
        //override the Awake method. But make sure to call base.Awake() first!
        protected override void Awake()
        {
                base.Awake(); // Required!

                // If the singleton was not created successfully,
                // perhaps because there is already an instance of
                // the singleton in the scene
                // the creationFailed variable will be true.
                if (creationFailed)
                        return;

                // Your initialization code here
        }
}
```

## Accessing your new Singleton

You can access your new Singleton like this:

```
MyManager.Instance.MyMethod();
```

## Automatic creation

If the singleton is not created, it will be created automatically. It will look for a Prefab with the same name as the class in the Resources folder, and if it finds one, it will instantiate it.

If such prefab doesn't exist, it will create an empty GameObject with the name of the class and attach the singleton script to it.
