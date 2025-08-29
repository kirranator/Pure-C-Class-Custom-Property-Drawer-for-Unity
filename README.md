# Pure C# Class Custom Property Drawer for Unity

This repository provides a **custom Unity property drawer** that allows you to serialize and edit **pure C# class references** directly in the Unity Inspector.  
Normally, Unity only serializes `MonoBehaviour` or `ScriptableObject` types, but with `[SerializeReference]` and this drawer, you can reference and configure subclasses of a base class dynamically.

---

## Features

- Dropdown in the inspector to pick a subclass of your base type  
- Foldout to expand and edit subclass fields   
- Supports base types, arrays, and generic lists  
- Automatically detects all concrete (non-abstract) subclasses in your project  

---

## Usage

### 1. Define a Base Class
Your base class must be a **pure C# class** (not `MonoBehaviour` or `ScriptableObject`) and marked `[Serializable]`.

```csharp
[Serializable]
public abstract class ActionBase
{
    public string actionName;
}
````

### 2. Define Subclasses

Add any number of subclasses. These will appear in the inspector dropdown.

```csharp
[Serializable]
public class JumpAction : ActionBase
{
    public float jumpForce;
}

[Serializable]
public class RunAction : ActionBase
{
    public float speed;
}
```

### 3. Reference It with `[SerializeReference]`

In a `MonoBehaviour` or `ScriptableObject`, declare a field for your base type:

```csharp
public class PlayerController : MonoBehaviour
{
    [SerializeReference] 
    public ActionBase currentAction;
}
```

### 4. Apply the Property Drawer

At the top of the drawer script, replace `CLASSNAMEHERE` with your base type:

```csharp
[CustomPropertyDrawer(typeof(ActionBase), true)]
public class CraftActionsPropertyDrawer : PropertyDrawer
```

Now, in the Inspector, you’ll see a dropdown that lets you pick `JumpAction`, `RunAction`, or any other subclass. You can expand it to edit its fields directly.

---

## Example

In the Inspector, the `currentAction` field will look like this:

```
▶ currentAction       [JumpAction ▼]
```

Expanding it shows the serialized fields of the selected subclass:

```
▼ currentAction       [RunAction ▼]
    speed: 5
```
