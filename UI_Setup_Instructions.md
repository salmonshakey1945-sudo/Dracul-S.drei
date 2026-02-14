# UI Setup Instructions

Since the AI cannot create GameObjects in the scene, please perform the following steps to set up the HUD:

1.  **Create a Canvas:**
    *   Right-click in Hierarchy > `UI` > `Canvas`.
    *   Set **Render Mode** to `Screen Space - Overlay`.
    *   Set **UI Scale Mode** in `Canvas Scaler` to `Scale With Screen Size` (Reference Resolution: 1920 x 1080).

2.  **Create Sliders (Health & Blood):**
    *   Right-click on Canvas > `UI` > `Slider`. Name it `HealthBar`.
    *   Right-click on Canvas > `UI` > `Slider`. Name it `BloodBar`.
    *   Position them on the screen (e.g., Top-Left corner).
    *   *Optional:* Change the "Fill" color of `HealthBar` to Green/White and `BloodBar` to Red.

3.  **Setup UIManager:**
    *   Create an Empty GameObject in the scene (Name: `GameManagers`).
    *   Attach the `UIManager` script (located in `Assets/Scripts/Core/UIManager.cs`) to it.
    *   **Drag & Drop** the `HealthBar` and `BloodBar` sliders into the corresponding slots in the Inspector.
    *   The `Player Stats` field should auto-detect, but you can manually drag your Player object there to be safe.

4.  **Test:**
    *   Play the game.
    *   The Blood Bar should slowly decrease over time.
