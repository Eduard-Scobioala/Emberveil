
// The interface that all savable components (Player, Enemy, etc.) will implement.
public interface ISavable
{
    // A unique identifier for this object in the scene
    // Crucial for matching save data to the correct GameObject on load.
    string GetUniqueIdentifier();

    // Gathers the object's current state and returns it as a generic object.
    object CaptureState();

    // Restores the object's state from the provided data.
    void RestoreState(object state);
}