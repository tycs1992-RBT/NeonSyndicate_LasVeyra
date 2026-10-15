# Technical references checked for Build 02G

These are background API references, not evidence that the update was compiled or playtested.

- Unity JsonUtility.FromJson, plain serializable data objects and missing-field handling: https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/jsonutility/fromjson
- Unity JSON serialization, fields and supported types: https://docs.unity.com/en-us/engine/6000.0/manual/scripting/compilation-and-code-reload/script-serialization/json-serialization
- Ground probing, collision masks, inventory methods, UI hooks and controller input follow the exact implementations already present in the supplied game source. Native compatibility must be tested with the project's pinned Editor.

The restore code does not rely only on a field initializer being invoked: a null Discovery block and null Entries list are explicitly accepted as old-save defaults.
