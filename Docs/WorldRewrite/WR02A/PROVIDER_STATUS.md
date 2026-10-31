# External generation provider status

Verified 2026-10-28 against official Tripo documentation. No connected Tripo tool or TRIPO_API_KEY was present. No provider request, CLI login, paid generation, credit consumption or provider-generated mesh occurred. The two demonstrations are original local Blender Python production with AI assistance.

The optional external-only adapter uses the v3 capability endpoints, an explicitly selected account region, environment-variable credentials, bounded task polling, quarantined GLB downloads, original hashes and task receipts. It never places a credential or provider client in Assets or Unity player code. A timeout during POST preserves an exclusive request reservation and never automatically submits a second paid task. GET operations have bounded retries. Signed download URLs and raw provider errors are not persisted or printed.

No request is authorized by a key merely being configured. Paid submission requires an exact request hash, reviewed pricing date, future expiration, explicit owner approval, positive credit ceiling and acknowledgement that the published API does not expose a request-level hard spending cap. The local estimate is an admission limit, not a promise of provider-side billing enforcement. The pricing snapshot expires rather than silently reusing an old rate. There is no paid batch submitter.

Official references inspected:

- API: https://developers.tripo3d.ai/en/docs/introduction
- Text generation: https://developers.tripo3d.ai/en/docs/generation-text-to-model/standard
- Image generation: https://developers.tripo3d.ai/en/docs/generation-image-to-model/standard
- Task polling: https://developers.tripo3d.ai/en/docs/task-query
- Billing: https://developers.tripo3d.ai/en/docs/billing
- Published pricing: https://developers.tripo3d.ai/en/pricing
- Official SDK: https://github.com/Tripo-Developer/tripo-python-sdk
- CLI: https://developers.tripo3d.ai/en/docs/cli
- Blender/Unity plugins: https://developers.tripo3d.ai/en/docs/plugins

The SDK documentation identifies its 1.x release as v3 and schedules v2 retirement for 2026-10-31. This adapter does not use v2. The documented CLI requires Node 20 or later and explicit account region selection; it was not installed or authenticated here. The plugin page lists Blender plugin 0.7.3 for Blender 3.0+ and Unity plugin 1.0.1 for Unity 2022+. Those are documentation compatibility statements, not verified Unity 6000.3.16f1 operation. Neither plugin was installed in the project; Packages and renderer assignments remain unchanged.

The inspected pricing table gives H-series standard-texture base estimates of 20 credits for text and 30 for image, with detailed texture adding 10, extreme texture adding 20 and detailed geometry adding 20. The adapter permits only its quoted model/options and rejects unknown additions. Prices and provider terms must be rechecked before approving a real request. No estimate authorizes payment.

For later use, set TRIPO_API_KEY and TRIPO_REGION locally, outside Git and Unity. Use region ov for an explicitly selected international account or cn for an explicitly selected mainland account. Do not infer the account site or paste keys into chat. Run doctor for a read-only balance check, then quote a concrete brief. Image generation requires a separately authorized uploaded file token and source-rights approval. The adapter does not automatically upload unapproved references.
