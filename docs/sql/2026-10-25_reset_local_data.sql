-- 2026-10-25 清空本地业务数据（保留地区）
--
-- 用途：本地联调前把库恢复成「只有地区」的干净状态。
-- 执行后重启后端，种子逻辑会自动补齐角色、管理员、字典和少量样例门店/房型/订单。
--
--   mysql -h 127.0.0.1 -u <user> -p <database> < 2026-10-25_reset_local_data.sql
--
-- 说明：
--   1. 只保留 Regions（省市区数据），其余表全部清空，包括管理员账号。
--      重启后会用 Seed:AdminPassword 或默认口令重建 admin，并标记为「首次登录必须改密」。
--   2. 开发环境（appsettings.Development.json 默认 Database:Provider=InMemory）不落盘，
--      直接重启后端就是干净数据，不需要执行本脚本。
--   3. 生产库不要执行本脚本。

SET FOREIGN_KEY_CHECKS = 0;

DROP PROCEDURE IF EXISTS tmp_reset_business_data;

DELIMITER $$
CREATE PROCEDURE tmp_reset_business_data()
BEGIN
    DECLARE finished INT DEFAULT 0;
    DECLARE table_name_value VARCHAR(128);
    DECLARE table_cursor CURSOR FOR
        SELECT TABLE_NAME
        FROM information_schema.TABLES
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_TYPE = 'BASE TABLE'
          AND TABLE_NAME NOT IN ('Regions', '__EFMigrationsHistory');
    DECLARE CONTINUE HANDLER FOR NOT FOUND SET finished = 1;

    OPEN table_cursor;
    clear_loop: LOOP
        FETCH table_cursor INTO table_name_value;
        IF finished = 1 THEN
            LEAVE clear_loop;
        END IF;
        SET @statement = CONCAT('DELETE FROM `', table_name_value, '`');
        PREPARE stmt FROM @statement;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END LOOP;
    CLOSE table_cursor;
END$$
DELIMITER ;

CALL tmp_reset_business_data();
DROP PROCEDURE tmp_reset_business_data;

SET FOREIGN_KEY_CHECKS = 1;

SELECT COUNT(*) AS remaining_regions FROM `Regions`;
