# Imported visual startup correction

The owner reported CreateImpl from the ImportedVisual04OD MonoBehaviour constructor or field initializer. Source inspection confirms a MaterialPropertyBlock allocation in a field initializer, so adding the visual adapter can request a Unity native object during component construction even when no imported model exists.

The allocation now happens only inside Initialize, reached from Start or the existing LateUpdate bond-readiness path, after a legitimate enabled model is found. Pre-clone component/controller validation and fallback restoration are retained. No model, authority, save, reward or controller is replaced.

Executed on the updated source: a genuine Editor/classic/Built-in compile against the actual bundled Custom NUnit passed, a separate external player compile passed, and ImportedVisualLifecycleH3Tests passed 1/1. The regression reads actual compiled constructor IL and rejects Unity native allocations there. This is not native proof that the owner's Console is cleared.

Native Unity import/PlayMode and owner reimport confirmation: NOT RUN. The previous UPM IPC blocker is unchanged; no repeat launch was attempted. Close the Editor, Fetch then Pull the art branch, reopen Unity 6000.3.16f1, clear the Console and inspect the first remaining error. No merge is needed.
