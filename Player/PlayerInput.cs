using UnityEngine;

public static class PlayerInput {

    static bool allowMovementInput = true;
	static bool usingController = PlayerPrefsManager.GetController();

    public static Vector2 GetDirectionalInput()
    {
        if(!allowMovementInput) {
            return Vector2.zero;
        }

		Vector2 output;
        if(Input.GetAxisRaw("Left Joystick Horizontal") != 0 || Input.GetAxisRaw("Left Joystick Horizontal") != 0) {
            usingController = true;
			PlayerPrefsManager.SetController (true);
            output = new Vector2(Input.GetAxisRaw("Left Joystick Horizontal"), -Input.GetAxisRaw("Left Joystick Vertical"));
        } else  if(Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0) {
                usingController = false;
			PlayerPrefsManager.SetController (false);
                output = new Vector2(Input.GetAxisRaw("Horizontal"), -Input.GetAxisRaw("Vertical"));
        } else {
            return Vector2.zero;
        }

		float magnitude = output.magnitude;
		if (magnitude > 1f) {
			output /= magnitude;
		}
		return output;
    }

    public static bool GetJumpDown() {
        if(Input.GetButtonDown("A Button")) {
            usingController = true;
			PlayerPrefsManager.SetController (true);
            return true;
        } else if(Input.GetKeyDown(KeyCode.Space)) {
            usingController = false;
			PlayerPrefsManager.SetController (false);
            return true;
        } else {
            return false;
        }
    }

    public static bool GetJumpUp()
    {
        if (Input.GetButtonUp("A Button")) {
            usingController = true;
			PlayerPrefsManager.SetController (true);
            return true;
        } else if (Input.GetKeyUp(KeyCode.Space)) {
            usingController = false;
			PlayerPrefsManager.SetController (false);
            return true;
        } else {
            return false;
        }
    }

	public static bool GetFlyDown() {
		if (Input.GetButtonDown("X Button")) {
			usingController = true;
			PlayerPrefsManager.SetController (true);
			return true;
		} else if (Input.GetMouseButtonDown(1)) {
			usingController = false;
			PlayerPrefsManager.SetController (false);
			return true;
		} else {
			return false;
		}
	}

	public static bool GetFlyUp() {
		if (Input.GetButtonUp("X Button")) {
			usingController = true;
			PlayerPrefsManager.SetController (true);
			return true;
		} else if (Input.GetMouseButtonUp(1)) {
			usingController = false;
			PlayerPrefsManager.SetController (false);
			return true;
		} else {
			return false;
		}
	}

    public static bool UsingTool()
    {
        if (Input.GetButton("B Button")) {
            usingController = true;
			PlayerPrefsManager.SetController (true);
            return true;
        } else if (Input.GetMouseButton(0)) {
            usingController = false;
			PlayerPrefsManager.SetController (false);
            return true;
        } else {
            return false;
        }
    }

    public static bool StaredtUsingTool() {
        return ((Input.GetButtonDown("X Button") || Input.GetMouseButtonDown(0)) && allowMovementInput);
    }

    public static bool StoppedUsingTool()
    {
        return ((Input.GetButtonUp("X Button") || Input.GetMouseButtonUp(0)) && allowMovementInput);
    }
		
    public static void StartMovementInput() {
        allowMovementInput = true;
    }

    public static void StopMovementInput() {
        allowMovementInput = false;
    }

    public static bool NextLine() {
        if (Input.GetButtonDown("A Button")) {
            usingController = true;
			PlayerPrefsManager.SetController (true);
            return true;
        } else if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)) {
            usingController = false;
			PlayerPrefsManager.SetController (false);
            return true;
        } else {
            return false;
        }
    }

    public static bool NextSong() {
        return (Input.GetButtonDown("Right Joystick Push") || Input.GetKeyDown(KeyCode.Greater));
    }

    public static bool UsingController() {
        return usingController;
    }
}
