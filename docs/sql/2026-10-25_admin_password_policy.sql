-- 2026-10-25 管理员密码策略
--
-- 背景：项目用 EnsureCreated 建库，没有 EF 迁移；已有的 MySQL 库不会自动加字段。
-- 升级到本次代码前，先在业务库执行一遍本脚本（幂等，可重复执行）。
--
--   mysql -h <host> -u <user> -p <database> < 2026-10-25_admin_password_policy.sql
--
-- 执行后：所有管理员的 MustChangePassword 默认为 0（不受影响），
-- 只有新建管理员或被超管重置密码的账号会被置为 1，下次登录强制改密。

SET @schema := DATABASE();

SET @sql := (
    SELECT IF(
        COUNT(*) = 0,
        'ALTER TABLE `AdminUsers` ADD COLUMN `MustChangePassword` tinyint(1) NOT NULL DEFAULT 0',
        'SELECT ''MustChangePassword already exists'''
    )
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = 'AdminUsers' AND COLUMN_NAME = 'MustChangePassword'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        COUNT(*) = 0,
        'ALTER TABLE `AdminUsers` ADD COLUMN `PasswordChangedAt` datetime(6) NULL',
        'SELECT ''PasswordChangedAt already exists'''
    )
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = @schema AND TABLE_NAME = 'AdminUsers' AND COLUMN_NAME = 'PasswordChangedAt'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- 建议顺手把内置 admin 账号标记为「必须改密」，避免沿用初始口令。
UPDATE `AdminUsers`
SET `MustChangePassword` = 1
WHERE `Username` = 'admin'
  AND `PasswordChangedAt` IS NULL;
