-- VanderClub: áreas fixas no código e pagamentos simulados no Mobile.
-- Execute no banco EXISTENTE, com a API parada e uma cópia de segurança feita.
-- Não execute novamente o script completo de criação para aplicar esta atualização.
-- Nenhuma conta, reserva, mensalidade ou pagamento existente é excluído.
-- MySQL 8.4. Use uma conta administradora do banco (por exemplo, root).

USE vanderclub;
SET NAMES utf8mb4 COLLATE utf8mb4_0900_ai_ci;
SET SESSION time_zone = '+00:00';

DELIMITER $$
DROP PROCEDURE IF EXISTS vc_atualizar_20261104$$
CREATE PROCEDURE vc_atualizar_20261104()
SQL SECURITY INVOKER
BEGIN
    DECLARE bloqueio INT DEFAULT 0;
    DECLARE modo_seguro INT DEFAULT 0;
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        SET SESSION sql_safe_updates = modo_seguro;
        DO RELEASE_LOCK('vanderclub_atualizacao_20261104');
        RESIGNAL;
    END;

    SET modo_seguro = @@SESSION.sql_safe_updates;
    SET SESSION sql_safe_updates = 0;
    IF NOT EXISTS (SELECT 1 FROM schema_migrations WHERE codigo='vanderclub-1.0.0' AND estado='PRONTO') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='O banco original VanderClub precisa estar instalado e pronto.';
    END IF;
    SELECT GET_LOCK('vanderclub_atualizacao_20261104', 10) INTO bloqueio;
    IF bloqueio <> 1 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Outra atualizacao esta em andamento.';
    END IF;
    INSERT INTO schema_migrations(codigo,estado)
    VALUES ('vanderclub-1.0.1','INSTALANDO')
    ON DUPLICATE KEY UPDATE estado='INSTALANDO',concluida_em=NULL;

    -- A referência antiga permanece para preservar o histórico e suas FKs.
    -- Novas ocupações usam somente o ID do catálogo fixo + a unidade.
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
        WHERE table_schema=DATABASE() AND table_name='ocupacoes_area' AND column_name='area_padrao_id') THEN
        ALTER TABLE ocupacoes_area ADD COLUMN area_padrao_id SMALLINT UNSIGNED NULL AFTER area_id;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns
        WHERE table_schema=DATABASE() AND table_name='ocupacoes_area' AND column_name='area_id' AND is_nullable='NO') THEN
        ALTER TABLE ocupacoes_area MODIFY COLUMN area_id INT NULL;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_schema=DATABASE() AND table_name='ocupacoes_area' AND constraint_name='fk_ocup_clube') THEN
        ALTER TABLE ocupacoes_area ADD CONSTRAINT fk_ocup_clube FOREIGN KEY (clube_id) REFERENCES clubes(id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_schema=DATABASE() AND table_name='ocupacoes_area' AND constraint_name='ck_ocup_area_padrao') THEN
        ALTER TABLE ocupacoes_area ADD CONSTRAINT ck_ocup_area_padrao
            CHECK ((area_padrao_id IS NULL OR area_padrao_id BETWEEN 1 AND 9)
                AND (area_id IS NOT NULL OR area_padrao_id IS NOT NULL));
    END IF;
    IF NOT EXISTS (SELECT 1 FROM information_schema.statistics
        WHERE table_schema=DATABASE() AND table_name='ocupacoes_area' AND index_name='ix_ocup_padrao') THEN
        ALTER TABLE ocupacoes_area ADD INDEX ix_ocup_padrao (clube_id,area_padrao_id,status,inicio_em,fim_em);
    END IF;

    -- Reservas/eventos antigos continuam bloqueando o recurso correspondente.
    -- Áreas de outros tipos permanecem legadas, sem exclusão ou conversão forçada.
    UPDATE ocupacoes_area o
    JOIN areas a ON a.id=o.area_id
    JOIN tipos_area t ON t.id=a.tipo_area_id
    SET o.area_padrao_id=CASE t.codigo
        WHEN 'ACADEMIA' THEN 1 WHEN 'FUTEBOL' THEN 2 WHEN 'POLIESPORTIVA' THEN 3
        WHEN 'AREIA' THEN 4 WHEN 'TENIS' THEN 5 WHEN 'JOGOS' THEN 6
        WHEN 'SALAO_SOCIAL' THEN 7 WHEN 'SALAO_FESTA' THEN 8 WHEN 'QUIOSQUE' THEN 9 END
    WHERE o.area_padrao_id IS NULL
        AND t.codigo IN ('ACADEMIA','FUTEBOL','POLIESPORTIVA','AREIA','TENIS','JOGOS','SALAO_SOCIAL','SALAO_FESTA','QUIOSQUE');

    -- A origem distingue claramente simulação de recebimento real.
    -- DROP/ADD ocorre na mesma instrução ALTER, sem uma janela sem validação.
    IF EXISTS (SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_schema=DATABASE() AND table_name='pagamentos' AND constraint_name='ck_pag_origem') THEN
        ALTER TABLE pagamentos DROP CHECK ck_pag_origem,
            ADD CONSTRAINT ck_pag_origem CHECK (origem IN ('MANUAL','PROVEDOR','SIMULADO'));
    ELSE
        ALTER TABLE pagamentos ADD CONSTRAINT ck_pag_origem CHECK (origem IN ('MANUAL','PROVEDOR','SIMULADO'));
    END IF;

    CREATE OR REPLACE SQL SECURITY INVOKER VIEW vw_reservas_detalhadas AS
    SELECT r.*,o.area_id,o.area_padrao_id,o.clube_id,o.inicio_em,o.fim_em,
           o.intervalo_apos_minutos,o.status AS status_ocupacao,a.nome AS area_nome
    FROM reservas r JOIN ocupacoes_area o ON o.reserva_id=r.id
    LEFT JOIN areas a ON a.id=o.area_id;

    UPDATE schema_migrations SET estado='PRONTO',concluida_em=UTC_TIMESTAMP(6)
    WHERE codigo='vanderclub-1.0.1';
    SET SESSION sql_safe_updates = modo_seguro;
    DO RELEASE_LOCK('vanderclub_atualizacao_20261104');
END$$
CALL vc_atualizar_20261104()$$
DROP PROCEDURE vc_atualizar_20261104$$
DELIMITER ;

SELECT codigo,estado,concluida_em FROM schema_migrations WHERE codigo='vanderclub-1.0.1';
