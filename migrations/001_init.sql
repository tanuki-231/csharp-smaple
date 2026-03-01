CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY,
    user_id VARCHAR(100) UNIQUE NOT NULL,
    password_hash VARCHAR(500) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL
);

CREATE TABLE IF NOT EXISTS todos (
    id UUID PRIMARY KEY,
    user_ref_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    title VARCHAR(200) NOT NULL,
    description VARCHAR(2000) NOT NULL DEFAULT '',
    status VARCHAR(20) NOT NULL CHECK (status IN ('pending', 'in_progress', 'done')),
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_todos_user_created_at ON todos (user_ref_id, created_at DESC);

CREATE TABLE IF NOT EXISTS todo_attachments (
    id UUID PRIMARY KEY,
    todo_ref_id UUID NOT NULL REFERENCES todos(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    size BIGINT NOT NULL,
    type VARCHAR(100) NOT NULL,
    s3_key TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_todo_attachments_todo_ref_id ON todo_attachments (todo_ref_id);

ALTER TABLE todo_attachments
    ADD COLUMN IF NOT EXISTS s3_key TEXT;

ALTER TABLE todo_attachments
    DROP COLUMN IF EXISTS data_url;
