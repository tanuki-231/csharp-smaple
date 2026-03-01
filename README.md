# TODO Backend (.NET 8 + PostgreSQL + Redis)

Reactフロントエンドから利用するTODO管理バックエンドです。以下を満たします。

- ASP.NET Core Web API
- PostgreSQL永続化（Aurora PostgreSQL互換）
- Redisセッション管理（Bearer Token / TTL 20分）
- Docker / docker-compose で即起動
- `demo / password` シードユーザーで即疎通確認

## アーキテクチャ

- Controller → Service → Repository / Infrastructure
- `users`, `todos` を PostgreSQL で管理
- ログイン成功時にランダムトークンを発行し Redis に保存（TTLは20分）
- 認証付き API は Redis 上の token を検証。なければ `440 session timeout`
- 例外応答はグローバルミドルウェアで統一

## リポジトリ構成

```text
.
├── Dockerfile
├── README.md
├── TodoApi.sln
├── docker-compose.yml
├── migrations
│   └── 001_init.sql
├── src
│   └── TodoApi
│       ├── Application
│       │   └── Contracts.cs
│       ├── Controllers
│       │   ├── AuthController.cs
│       │   └── TodosController.cs
│       ├── Domain
│       │   ├── Entities
│       │   │   ├── TodoItem.cs
│       │   │   └── User.cs
│       │   ├── Enums
│       │   │   └── TodoStatus.cs
│       │   └── Exceptions
│       │       └── AppException.cs
│       ├── Infrastructure
│       │   ├── Data
│       │   │   ├── AppDbContext.cs
│       │   │   ├── DbMigrationRunner.cs
│       │   │   └── DbSeeder.cs
│       │   ├── Repositories
│       │   │   ├── TodoRepository.cs
│       │   │   └── UserRepository.cs
│       │   ├── Security
│       │   │   └── PasswordHasherService.cs
│       │   └── Sessions
│       │       └── RedisSessionService.cs
│       ├── Middleware
│       │   ├── GlobalExceptionMiddleware.cs
│       │   └── TokenAuthenticationMiddleware.cs
│       ├── Models
│       │   ├── Requests
│       │   │   ├── LoginRequest.cs
│       │   │   └── TodoUpsertRequest.cs
│       │   └── Responses
│       │       ├── LoginResponse.cs
│       │       └── TodoResponse.cs
│       ├── Services
│       │   ├── AuthService.cs
│       │   └── TodoService.cs
│       ├── Program.cs
│       ├── TodoApi.csproj
│       └── appsettings.json
└── tests
    └── TodoApi.Tests
        ├── TodoApi.Tests.csproj
        └── TodoServiceTests.cs
```

## API仕様（/api）

- `POST /api/login`（認証）
- `DELETE /api/logout`（任意実装）
- `GET /api/todos`
- `POST /api/todos`
- `PUT /api/todos/{id}`
- `DELETE /api/todos/{id}`

エラー分類:

- auth: 401 / 403
- session_timeout: 440（本実装 / Redis TTL切れ）
- unexpected: その他（例: 404 / 500）

## 添付ファイル仕様

- 添付は任意
- 1 TODO あたり最大 3 ファイル
- 1 ファイルあたり最大 1MB
- `attachments` 配列で JSON として送信（`multipart/form-data` は使わない）

## 起動方法（Docker）

```bash
docker compose up --build
```

起動後:

- API: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`

## 動作確認（curl）

### 1. ログイン

```bash
curl -s -X POST http://localhost:8080/api/login \
  -H "Content-Type: application/json" \
  -d '{"userId":"demo","password":"password"}'
```

レスポンス例:

```json
{"token":"<TOKEN>","userId":"demo"}
```

### 2. TODO追加

```bash
TOKEN=<TOKEN>
curl -s -X POST http://localhost:8080/api/todos \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{"title":"first todo","description":"sample","status":"pending","attachments":[{"name":"sample.txt","size":12,"type":"text/plain","dataUrl":"data:text/plain;base64,SGVsbG8gV29ybGQh"}]}'
```

### 3. TODO一覧取得

```bash
curl -s http://localhost:8080/api/todos \
  -H "Authorization: Bearer ${TOKEN}"
```

レスポンス例:

```json
[
  {
    "id": "string",
    "title": "string",
    "description": "string",
    "status": "pending",
    "attachmentCount": 1
  }
]
```
### 4. TODO更新

```bash
TODO_ID=<ID>
curl -s -X PUT http://localhost:8080/api/todos/${TODO_ID} \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d "{\"id\":\"${TODO_ID}\",\"title\":\"updated\",\"description\":\"changed\",\"status\":\"in_progress\",\"attachments\":[{\"id\":\"<ATTACHMENT_ID>\",\"name\":\"sample.txt\",\"size\":12,\"type\":\"text/plain\",\"dataUrl\":\"data:text/plain;base64,SGVsbG8gV29ybGQh\"}]}"
```

### 5. TODO削除

```bash
curl -i -X DELETE http://localhost:8080/api/todos/${TODO_ID} \
  -H "Authorization: Bearer ${TOKEN}"
```

## 主要環境変数

- `ConnectionStrings__PostgreSQL` : PostgreSQL接続文字列
- `Redis__Connection` : Redis接続先
- `Session__TTLMinutes` : セッションTTL（分、デフォルト20）
- `Cors__AllowedOrigins__0` : CORS許可オリジン
- `AWS__Enabled` : `true` の場合に SSM/SecretsManager 取得を有効化
- `AWS__Region` : AWSリージョン
- `AWS__SsmPrefix` : SSMのパスプレフィックス（例 `/todo-api/`）
- `AWS__DbSecretId` : DB接続情報を保持するSecretsManagerのシークレットID
- `AWS__LogGroup` : CloudWatch Logsのロググループ


## AWS連携（ECS/Fargate想定）

本実装では `AWS:Enabled=true` の場合、起動時に以下を取得します。

- **SSM Parameter Store** (`AWS:SsmPrefix`, 例 `/todo-api/`)
  - `RedisConnection` → `Redis:Connection`
  - `SessionTTLMinutes` → `Session:TTLMinutes`（未設定時は20）
  - `AllowedOrigins`（カンマ区切り）→ `Cors:AllowedOrigins`
  - `CloudWatchLogGroup` → `AWS:LogGroup`
- **Secrets Manager** (`AWS:DbSecretId`)
  - `connectionString` または `host/port/username/password/dbname` 形式のJSONを読み取り、`ConnectionStrings:PostgreSQL` を生成

CloudWatch Logs には `AWS.Logger.AspNetCore` を使って出力します。

## DBマイグレーション

- `migrations/001_init.sql` を起動時に `DbMigrationRunner` が自動適用
- 何度実行しても安全な `IF NOT EXISTS` を利用

## 補足

- パスワードは `PasswordHasher<TUser>` によるハッシュで保存（平文禁止）
- Nullable reference types 有効
- async/await ベースで非同期処理
- DTO と Entity を分離






