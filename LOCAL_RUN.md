# Local integrated execution

Run commands from the organization root (the directory containing all repositories). Linux, Docker Snap, Python 3, .NET SDK 10, Java 21/Maven and the existing Android SDK/JDK toolchain are required. Keep the provisioned Supabase project, 18 migrations, private buckets, local containers and volumes. No migration is run by these commands.

## Protected configuration

Keep files private (600) and directories private (700). Do not source configuration files as shell scripts or copy them into Git.

- `~/.config/disaster-management-platform/{disasters-original.connection,reconstruction.connection,integration.properties,damage-assessment.properties,supabase-ca.crt}`: separate runtime credentials, Session Pooler and PostgreSQL `verify-full` CA.
- `~/.config/disaster-management-platform/{evidence-storage.properties,reconstruction-storage.env}`: separate Storage identities; never use administrative keys in runtime.
- `config/keycloak/runtime.env`, `config/keycloak/certificates/`, `config/task10/trust.jks`: existing issuer, credentials and trust material. The Damage JVM truststore preserves the image’s standard public CAs and adds the local Keycloak certificate; a Keycloak-only truststore prevents Storage HTTPS. `KC_PUBLIC_URL`, `KC_DB_PASSWORD`, `KC_DB_ADMIN_PASSWORD`, `KC_CERTIFICATE_DIR`, `KC_RAZOR_REDIRECT_URI`, `KC_ANDROID_REDIRECT_URI`, `KC_EDEN_REDIRECT_URI`.
- `config/delivery-validation/delegations.json`: existing organization-scoped Eden delegations; no administrator runtime identity.
- `config/task10/{disasters,integration,damage,reconstruction,gateway,razor}.env`: existing integrated runtime profiles. Do not replace them with isolated test database profiles.
- Razor: `ASPNETCORE_URLS`, `ASPNETCORE_Kestrel__Certificates__Default__Path`, `ASPNETCORE_Kestrel__Certificates__Default__KeyPath`, `SSL_CERT_FILE`, `Api__UseMock=false`, `Api__GatewayUrl`, `Api__EdenUrl`, `Security__Authority`, `Security__ClientId`, `Security__ClientSecret`, `Security__DataProtectionPath`, `TZ=America/Bogota`. The persistent key directory is shared only by presentation replicas.
- Java: `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_PUBLIC_KEY_LOCATION`, domain `DAMAGE_DB_*` / `INTEGRATION_DB_*`, CA/pool/timeouts, `DISASTERS_BASE_URL`, `DISASTERS_OAUTH_*` / `DISASTERS_READ_OAUTH_*` / `DISASTERS_REPORT_OAUTH_*`. Damage also needs `EVIDENCE_STORAGE_EMAIL`, `EVIDENCE_STORAGE_PASSWORD`, `EVIDENCE_STORAGE_PUBLISHABLE_KEY`, `EVIDENCE_STORAGE_BUCKET`.
- .NET: `ConnectionStrings__Disasters` (historical setting name, separate domain identity), `Security__Authority`, `Security__Audience`, `ExternalApis__*`, `ExternalApis__*__OAuth__TokenEndpoint/ClientId/ClientSecret`, `Attachments__Storage__*`, `Messaging__*`. Keep the existing explicitly simulated funding adapter identified as DEMO; configure real Damage and Disaster URLs.

## Start

The existing containers retain their profiles, readonly CA/delegation mounts and port bindings. Start dependencies first; do not recreate volumes or run `down -v`.

```bash
docker start platform-identity-database-1
docker start platform-identity-keycloak-1
docker start sahana-evaluation-default-1
docker start plataformadesastres_reconstruccion-rabbitmq-1
python3 disaster-platform-presentation/scripts/run_local.py start
dotnet restore disaster-platform-presentation
dotnet build disaster-platform-presentation --no-restore
python3 disaster-platform-presentation/scripts/run_local.py web
```

`web` runs in the foreground; keep that terminal open. Runtime env changes require recreating the affected container using its existing Dockerfile/profile/mounts, not merely `docker restart`. The script above intentionally starts the configured containers without replacing them. The validated backend images are `delivery-disaster-backend-offline:validated`, `task10-integration:validated`, `delivery-disaster-damage-assessmet-service:validated`, `delivery-reconstruction-business-layer:validated`, `task10-gateway:validated`; their Dockerfiles remain in their respective repositories. Do not rebuild unchanged backends just to start the platform. When changing Integration or Gateway source, rebuild only those affected images with their existing Dockerfiles:

```bash
docker build -t task10-integration:validated disaster-integration-service
docker build -t task10-gateway:validated disaster-api-gateway
```

Keep the existing protected mounts/profile/port bindings when replacing those containers; a new tag alone does not update a running container.

| Component | Local address |
| --- | --- |
| Keycloak issuer | `https://localhost:18443/realms/platform` |
| Eden native | `http://localhost:8001/eden_default` |
| Disaster Management | `http://127.0.0.1:18913` |
| Integration | `http://127.0.0.1:18911` |
| Damage Assessment | `http://127.0.0.1:18914` |
| Reconstruction | `http://127.0.0.1:18915` |
| Gateway, Web APIs | `http://127.0.0.1:18912` |
| Gateway, emulator | `http://10.0.2.2:5080/api/` |
| Razor | `https://localhost:5081` |

Trust the existing development CA; do not disable TLS verification. Keep all service ports loopback-only. This local setup is not a production deployment.

```bash
curl --fail http://127.0.0.1:18912/health
curl --fail http://127.0.0.1:18911/api/integracion/diagnostico/ready
curl --fail http://127.0.0.1:18914/api/evaluaciones/diagnostico/ready
curl --fail http://127.0.0.1:18915/api/reconstruccion/ready
curl --fail http://127.0.0.1:18913/api/tipos-desastre
```

## Clients and lifecycle

The seven enrolled demo users are stored in `~/.config/disaster-management-platform/demo-users.json` (600), including passwords and TOTP enrollment secrets. Obtain them locally from the environment administrator and import each seed into an authenticator that supports SHA-256, six digits and a 30-second period. Never paste them into chat, logs or Git. Accounts use organization 3 and explicit disaster grants 4–9:

| User | Minimum existing domain roles |
| --- | --- |
| `demo.admin` | disasters.admin, reconstruction.write, damage.admin, org.admin, org.create, people.write, coordination.write |
| `demo.coordinador` | org.read, coordination.write, reconstruction.read |
| `demo.evaluador` | damage.write |
| `demo.validador` | damage.admin (required by the existing validation contract) |
| `demo.humanitario` | people.write, org.read |
| `demo.reconstructor` | reconstruction.write |
| `demo.consulta` | reconstruction.read, people.read, coordination.read, org.read |

No realm-administrator or infrastructure roles are assigned to these accounts. Existing write roles also authorize the corresponding reads where the service policy specifies that. Do not expand roles merely to make every navigation link succeed.

The protected operational fixture map is `~/.config/disaster-management-platform/demo-data.json`. It records native/remote IDs and stable operation UUIDs for recovery. Do not rerun provisioning with new identities or delete the demo records. The original `[DEMO-CO]` disasters 4–9, twelve assessments and eighteen original history entries are retained; later API operations add their own legitimate history. Roles, `organization_id` and `disaster_id` grants must be explicitly assigned by that administrator. Read permissions alone do not permit validation, coordination or financial operations. Service identities need the corresponding disaster grant as well. Native Eden access additionally requires the explicitly provisioned native account/realm or verified issuer+subject SSO binding; a Razor login alone does not grant Eden ACLs. Existing data ownership/ACL remain authoritative.

Login at Razor. Register a synthetic disaster, assign its authorized organization, and select it using the disaster selector. Use Persons and Coordination for the existing Eden-backed contracts. Families, shelters, inventory and native organizational relations remain in Eden. Coordination requires a real native Eden project linked to the organization's corresponding EVENT; never substitute a Reconstruction UUID.

Create a Draft evaluation with active asset/category identifiers, known COP budget and optional real evidence. Validate it with an authorized reviewer. Move the disaster through authorized phases to Recovery/Reconstruction with required justifications. Generate proposals, select one, formalize the project with its funding source ID and contractual activities, confirm formalization, record activity reports with evidence/documents, and close when the real monitoring summary allows it. Unknown/unsupported actions are disabled. DEMO funding is simulated, not money allocated by a real provider.

Use the existing Android Studio run configuration or, with `ANDROID_HOME` set to the installed SDK:

```bash
"$ANDROID_HOME/emulator/emulator" -list-avds
# Launch one of the listed existing AVDs:
"$ANDROID_HOME/emulator/emulator" -avd "$AVD_NAME"
"$ANDROID_HOME/platform-tools/adb" wait-for-device
"$ANDROID_HOME/platform-tools/adb" install -r disaster-mobile-client/app/build/outputs/apk/debug/app-debug.apk
"$ANDROID_HOME/platform-tools/adb" shell monkey -p co.edu.javeriana.disasterplatform.disaster_mobile_client 1
```

Use the validated Debug identity flow with an externally issued current token; no token is embedded in the APK. Mobile GPS remains the source of latitude/longitude. Room/offline pending data and files remain on device; do not uninstall or clear application data.

## Stop and restart

Stop Razor with Ctrl-C. `python3 disaster-platform-presentation/scripts/run_local.py stop` stops only Task 10 APIs/Gateways. Restart them with `start` or `restart`, then run `web` again. Stop dependency containers only when other work is not using them. Do not delete their volumes, reset Supabase, remove buckets or clear Android data.


### Preserved demo records

`[DEMO-CO]` disasters 4–9 and their original assessments/history were not recreated or deleted. Armenia (4) and San Andrés (8) have real proposals derived from the existing validated assessments. Use the project list with those disaster selectors to find Draft, Monitoring and Closed examples. Names marked DEMO-CO, their people, native DVR families, shelters, care records, coordination and resource inventory are fictitious and retained. Funding remains explicitly simulated. Do not equate a native stock/request record with a completed shipment.

Family creation uses Eden's native DVR family workflow, with a realm-scoped `dvr/group_membership` permission; the platform template checks the viewed person and submitted member before native processing. Existing native people/resources identities remain in protected `~/.config/disaster-management-platform/eden-platform-test.json`, `eden-zip-coordination-test.json` and `eden-platform-resources-test.json`; they are not administrative runtime accounts. Obtain authorized native access from the environment administrator rather than reusing an admin account.

The connected MVC lifecycle, real Storage uploads/downloads, version conflicts and foreign-identity denial were checked through HTTP. A visual browser check remains pending when no browser is exposed by the execution environment. Historical anonymous Disaster API access and authorized rotation of the previously exposed administrative Storage key remain security debt; keep this setup local. The existing local profiles leave messaging workers disabled; this guide does not claim a running cross-domain message-delivery scenario or production readiness.
