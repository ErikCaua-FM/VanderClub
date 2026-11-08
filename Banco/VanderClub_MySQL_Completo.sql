-- VANDERCLUB | BANCO MYSQL | ESQUEMA 1.0.0 | 03/11/2026
-- Alvo: MySQL Community Server 8.4 LTS. Arquivo UTF-8.
-- Baseado nas especificacoes e no contrato de integracao Mobile/Web 2.0.
-- Validacao: execucao real em MySQL 8.4.7; 49 verificacoes aprovadas.
-- Inventario: 45 tabelas, 92 FK, 132 CHECK, 74 gatilhos e 4 vistas.
-- Validado o esquema; integracao com clientes e transacoes da API pendentes.
--
-- COMO EXECUTAR NO WINDOWS / MYSQL WORKBENCH
-- 1. Inicie o servico MySQL Server. Workbench e apenas o cliente.
-- 2. Abra a conexao LOCAL do MySQL correto, como root. Confira a porta:
--    se outro servidor usa 3306, use a porta definida na instalacao (ex.: 3307).
--    A porta pertence a conexao; nao e configurada por este arquivo SQL.
-- 3. File > Open SQL Script > este arquivo. Execute o ARQUIVO INTEIRO,
--    sem selecao de texto, pelo primeiro raio ou Ctrl+Shift+Enter.
--    Deixe desativada a opcao de continuar execucao apos erros.
-- 4. Ao terminar, confira a mensagem final e atualize o painel Schemas.
-- 5. O resultado de CREATE USER mostra uma senha aleatoria SOMENTE se a
--    conta tecnica vanderclub_api@127.0.0.1 for criada agora. Guarde-a em
--    local privado para a futura API. Nao coloque essa senha nos clientes.
--    Se a conta ja existia, sua senha/permissoes anteriores sao preservadas.
--
-- O QUE ESTE SCRIPT FAZ
-- Cria vanderclub, tabelas, FK, indices, CHECK, gatilhos, vistas de consulta,
-- catalogos de UFs/interesses/tipos de area e conta tecnica para a API.
-- Nao instala o MySQL, nao muda portas/servicos/firewall e nao toca em outros
-- schemas. Nao cria associados, pagamentos, clubes ou senhas ficticias.
-- O Presidente sera criado pelo bootstrap seguro da API, com hash de senha.
-- A conta MySQL vanderclub_api NAO e um usuario de login da aplicacao.
--
-- USO E REEXECUCAO
-- Destinado a banco vazio (ou a retomada/reexecucao desta mesma versao).
-- Nao e uma migracao de modelos anteriores nem importador do SQLite.
-- Nao contem DROP TABLE/DATABASE, TRUNCATE nem desativa FOREIGN_KEY_CHECKS.
-- IF NOT EXISTS nao corrige uma tabela editada manualmente. Se ja existir
-- outro modelo em vanderclub, pare e prepare uma migracao com backup.
-- DDL MySQL confirma cada etapa: se houver erro, as anteriores permanecem.
-- Feche a conexao com erro, corrija a causa e reexecute o mesmo arquivo.
-- Avisos de objeto ja existente sao normais na segunda execucao.
--
-- CONVENCOES
-- IDs INT positivos de 32 bits, iguais aos DTOs Mobile; Web usa string decimal.
-- Dinheiro BIGINT em centavos BRL (12990 = R$ 129,90), nunca FLOAT.
-- Instantes DATETIME(6) em UTC; API deve usar time_zone='+00:00' por conexao.
-- Nascimento, visitas, vencimentos e vigencia contratual usam DATE civil.
-- data_fim dos contratos e exclusiva. Todas as FK usam RESTRICT implicito.
-- versao e incrementada pelo banco em UPDATE, para concorrencia otimista.
-- Autorizacao por papel/unidade, CPF, fuso, cotas e regras transacionais
-- pertencem tambem a API: veja o contrato de implementacao ao fim do arquivo.
--
-- Referencias oficiais:
-- https://dev.mysql.com/doc/refman/8.4/en/create-table-foreign-keys.html
-- https://dev.mysql.com/doc/refman/8.4/en/create-table-check-constraints.html
-- https://dev.mysql.com/doc/refman/8.4/en/create-trigger.html
-- https://dev.mysql.com/doc/refman/8.4/en/create-user.html

SET NAMES utf8mb4 COLLATE utf8mb4_0900_ai_ci;
SET SESSION time_zone = '+00:00';
SET SESSION sql_mode = 'STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION,ONLY_FULL_GROUP_BY';

SELECT VERSION() AS servidor, @@hostname AS host, @@port AS porta, CURRENT_USER() AS conta;
CREATE DATABASE IF NOT EXISTS vanderclub CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
USE vanderclub;

DELIMITER $$
CREATE PROCEDURE IF NOT EXISTS vc_verificar_instalacao_v1()
SQL SECURITY INVOKER
BEGIN
    DECLARE n INT DEFAULT 0;
    DECLARE marcador INT DEFAULT 0;
    IF VERSION() NOT LIKE '8.4.%' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Use MySQL Community Server 8.4 LTS para este script.';
    END IF;
    IF GET_LOCK('vanderclub_instalacao_v1', 10) <> 1 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Outra instalacao VanderClub esta em andamento.';
    END IF;
    SELECT COUNT(*) INTO n FROM information_schema.tables
      WHERE table_schema='vanderclub' AND table_type='BASE TABLE';
    SELECT COUNT(*) INTO marcador FROM information_schema.tables
      WHERE table_schema='vanderclub' AND table_name='schema_migrations';
    IF n > 0 AND marcador = 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='vanderclub ja tem outro esquema. Nao executar sem migracao e backup.';
    END IF;
    IF marcador = 1 THEN
        SELECT COUNT(*) INTO marcador FROM schema_migrations
          WHERE codigo='vanderclub-1.0.0';
        IF marcador <> 1 AND n > 1 THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Versao do esquema desconhecida. Nao prosseguir.';
        END IF;
        SELECT COUNT(*) INTO n FROM schema_migrations WHERE codigo <> 'vanderclub-1.0.0';
        IF n > 0 THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Existem outras migracoes. Use o migrador da versao instalada.';
        END IF;
    END IF;
END$$
DELIMITER ;
CALL vc_verificar_instalacao_v1();

CREATE TABLE IF NOT EXISTS schema_migrations (
  codigo VARCHAR(80) NOT NULL PRIMARY KEY,
  estado VARCHAR(20) NOT NULL,
  iniciada_em DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  concluida_em DATETIME(6) NULL,
  CONSTRAINT ck_migrations_estado CHECK (estado IN ('INSTALANDO','PRONTO'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
INSERT INTO schema_migrations (codigo, estado)
SELECT 'vanderclub-1.0.0', 'INSTALANDO'
WHERE NOT EXISTS (SELECT 1 FROM schema_migrations WHERE codigo='vanderclub-1.0.0');

-- 1. LOCALIDADES, PESSOAS, ACESSO E DOCUMENTOS

-- Unidades federativas brasileiras
CREATE TABLE IF NOT EXISTS estados (
  id INT NOT NULL AUTO_INCREMENT,
  nome VARCHAR(60) NOT NULL,
  uf CHAR(2) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_estados_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  UNIQUE KEY uq_estados_uf (uf)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Unidades federativas brasileiras';

-- Municipios; cadastrar conforme as unidades e associados
CREATE TABLE IF NOT EXISTS cidades (
  id INT NOT NULL AUTO_INCREMENT,
  estado_id INT NOT NULL,
  nome VARCHAR(120) NOT NULL,
  codigo_ibge CHAR(7) CHARACTER SET ascii COLLATE ascii_bin NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_cidades_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  CONSTRAINT fk_cidade_estado FOREIGN KEY (estado_id) REFERENCES estados (id),
  UNIQUE KEY uq_cidade_estado_nome (estado_id, nome),
  UNIQUE KEY uq_cidade_ibge (codigo_ibge)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Municipios; cadastrar conforme as unidades e associados';

-- Unidades fisicas da rede
CREATE TABLE IF NOT EXISTS clubes (
  id INT NOT NULL AUTO_INCREMENT,
  nome VARCHAR(160) NOT NULL,
  sigla VARCHAR(20) NOT NULL,
  descricao TEXT NULL,
  cidade_id INT NOT NULL,
  logradouro VARCHAR(200) NOT NULL,
  numero VARCHAR(20) NOT NULL,
  complemento VARCHAR(100) NULL,
  bairro VARCHAR(100) NOT NULL,
  cep CHAR(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  telefone VARCHAR(25) NOT NULL,
  email VARCHAR(254) NOT NULL,
  imagem VARCHAR(500) NULL,
  fuso_horario VARCHAR(64) NOT NULL DEFAULT 'America/Sao_Paulo',
  status VARCHAR(20) NOT NULL DEFAULT 'ATIVO',
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_clubes_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  CONSTRAINT fk_clube_cidade FOREIGN KEY (cidade_id) REFERENCES cidades (id),
  UNIQUE KEY uq_clube_sigla (sigla),
  KEY ix_clube_nome (nome),
  CONSTRAINT ck_clube_status CHECK (status IN ('ATIVO','INATIVO'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Unidades fisicas da rede';

-- Pessoa natural; pode existir sem conta de acesso
CREATE TABLE IF NOT EXISTS pessoas (
  id INT NOT NULL AUTO_INCREMENT,
  nome VARCHAR(160) NOT NULL,
  cpf CHAR(11) CHARACTER SET ascii COLLATE ascii_bin NULL,
  nascimento DATE NOT NULL,
  telefone VARCHAR(25) NULL,
  email_contato VARCHAR(254) NULL,
  cidade_id INT NULL,
  logradouro VARCHAR(200) NULL,
  numero VARCHAR(20) NULL,
  complemento VARCHAR(100) NULL,
  bairro VARCHAR(100) NULL,
  cep CHAR(8) CHARACTER SET ascii COLLATE ascii_bin NULL,
  sexo VARCHAR(30) NULL,
  estado_civil VARCHAR(40) NULL,
  inativado_em DATETIME(6) NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_pessoas_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  CONSTRAINT fk_pessoa_cidade FOREIGN KEY (cidade_id) REFERENCES cidades (id),
  UNIQUE KEY uq_pessoa_cpf (cpf),
  KEY ix_pessoa_nome (nome)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Pessoa natural; pode existir sem conta de acesso';

-- Credenciais da aplicacao; hashes produzidos pela API
CREATE TABLE IF NOT EXISTS usuarios (
  id INT NOT NULL AUTO_INCREMENT,
  pessoa_id INT NOT NULL,
  email_login VARCHAR(254) NOT NULL,
  email_normalizado VARCHAR(254) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_as_ci NOT NULL,
  senha_hash VARCHAR(512) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  perfil VARCHAR(20) NOT NULL,
  status VARCHAR(20) NOT NULL DEFAULT 'PENDENTE',
  email_verificado_em DATETIME(6) NULL,
  deve_trocar_senha BOOLEAN NOT NULL DEFAULT FALSE,
  falhas_login INT NOT NULL DEFAULT 0,
  bloqueado_ate DATETIME(6) NULL,
  security_stamp CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  ultimo_login_em DATETIME(6) NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_usuarios_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  CONSTRAINT fk_usuario_pessoa FOREIGN KEY (pessoa_id) REFERENCES pessoas (id),
  UNIQUE KEY uq_usuario_pessoa (pessoa_id),
  UNIQUE KEY uq_usuario_email (email_normalizado),
  CONSTRAINT ck_usuario_perfil CHECK (perfil IN ('ASSOCIADO','ADM_LOCAL','ADM_GERAL')),
  CONSTRAINT ck_usuario_status CHECK (status IN ('PENDENTE','ATIVO','BLOQUEADO','INATIVO')),
  CONSTRAINT ck_usuario_troca CHECK (deve_trocar_senha IN (0,1)),
  CONSTRAINT ck_usuario_falhas CHECK (falhas_login >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Credenciais da aplicacao; hashes produzidos pela API';

-- Uma unidade por ADM Local; ADM Geral tem escopo global
CREATE TABLE IF NOT EXISTS administradores_clubes (
  usuario_id INT NOT NULL,
  clube_id INT NOT NULL,
  atribuido_por_usuario_id INT NOT NULL,
  ativo BOOLEAN NOT NULL DEFAULT TRUE,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_administradores_clubes_versao CHECK (versao >= 1),
  PRIMARY KEY (usuario_id),
  CONSTRAINT fk_admin_usuario FOREIGN KEY (usuario_id) REFERENCES usuarios (id),
  CONSTRAINT fk_admin_clube FOREIGN KEY (clube_id) REFERENCES clubes (id),
  CONSTRAINT fk_admin_autor FOREIGN KEY (atribuido_por_usuario_id) REFERENCES usuarios (id),
  CONSTRAINT ck_admin_ativo CHECK (ativo IN (0,1)),
  KEY ix_admin_clube (clube_id, ativo)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Uma unidade por ADM Local; ADM Geral tem escopo global';

-- Sessoes revogaveis; somente hash de token aleatorio
CREATE TABLE IF NOT EXISTS sessoes (
  id INT NOT NULL AUTO_INCREMENT,
  usuario_id INT NOT NULL,
  token_hash BINARY(32) NOT NULL,
  csrf_token_hash BINARY(32) NULL,
  plataforma VARCHAR(10) NOT NULL,
  dispositivo VARCHAR(200) NULL,
  expira_em DATETIME(6) NOT NULL,
  revogada_em DATETIME(6) NULL,
  ultima_utilizacao_em DATETIME(6) NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_sessoes_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  CONSTRAINT fk_sessao_usuario FOREIGN KEY (usuario_id) REFERENCES usuarios (id),
  UNIQUE KEY uq_sessao_token (token_hash),
  KEY ix_sessao_usuario (usuario_id, revogada_em, expira_em),
  KEY ix_sessao_expira (expira_em),
  CONSTRAINT ck_sessao_plataforma CHECK (plataforma IN ('MOBILE','WEB'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Sessoes revogaveis; somente hash de token aleatorio';

-- Tokens de uso unico; nenhum token em texto puro
CREATE TABLE IF NOT EXISTS tokens_usuario (
  id INT NOT NULL AUTO_INCREMENT,
  usuario_id INT NOT NULL,
  finalidade VARCHAR(30) NOT NULL,
  token_hash BINARY(32) NOT NULL,
  email_destino VARCHAR(254) NULL,
  expira_em DATETIME(6) NOT NULL,
  utilizado_em DATETIME(6) NULL,
  revogado_em DATETIME(6) NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_tokens_usuario_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  CONSTRAINT fk_token_usuario FOREIGN KEY (usuario_id) REFERENCES usuarios (id),
  UNIQUE KEY uq_token_hash (token_hash),
  KEY ix_token_expira (expira_em),
  CONSTRAINT ck_token_finalidade CHECK (finalidade IN ('VERIFICAR_EMAIL','RECUPERAR_SENHA','ATIVAR_CONTA'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Tokens de uso unico; nenhum token em texto puro';

-- Catalogo opcional de interesses
CREATE TABLE IF NOT EXISTS interesses (
  id INT NOT NULL AUTO_INCREMENT,
  nome VARCHAR(80) NOT NULL,
  ativo BOOLEAN NOT NULL DEFAULT TRUE,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_interesses_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  UNIQUE KEY uq_interesse_nome (nome),
  CONSTRAINT ck_interesse_ativo CHECK (ativo IN (0,1))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Catalogo opcional de interesses';

-- Interesses da pessoa sem lista separada por virgulas
CREATE TABLE IF NOT EXISTS pessoas_interesses (
  pessoa_id INT NOT NULL,
  interesse_id INT NOT NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (pessoa_id, interesse_id),
  CONSTRAINT fk_pi_pessoa FOREIGN KEY (pessoa_id) REFERENCES pessoas (id),
  CONSTRAINT fk_pi_interesse FOREIGN KEY (interesse_id) REFERENCES interesses (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Interesses da pessoa sem lista separada por virgulas';

-- Metadados; binarios ficam em armazenamento privado
CREATE TABLE IF NOT EXISTS arquivos (
  id INT NOT NULL AUTO_INCREMENT,
  enviado_por_usuario_id INT NOT NULL,
  chave_storage VARCHAR(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
  nome_original VARCHAR(255) NOT NULL,
  mime_type VARCHAR(100) NOT NULL,
  tamanho_bytes BIGINT NOT NULL,
  sha256 BINARY(32) NOT NULL,
  status VARCHAR(20) NOT NULL DEFAULT 'PENDENTE',
  removido_em DATETIME(6) NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_arquivos_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  CONSTRAINT fk_arquivo_autor FOREIGN KEY (enviado_por_usuario_id) REFERENCES usuarios (id),
  UNIQUE KEY uq_arquivo_storage (chave_storage),
  CONSTRAINT ck_arquivo_tamanho CHECK (tamanho_bytes > 0),
  CONSTRAINT ck_arquivo_status CHECK (status IN ('PENDENTE','DISPONIVEL','REJEITADO','REMOVIDO'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Metadados; binarios ficam em armazenamento privado';

-- Texto exato e versionado dos documentos aceitos
CREATE TABLE IF NOT EXISTS documentos (
  id INT NOT NULL AUTO_INCREMENT,
  tipo VARCHAR(30) NOT NULL,
  codigo_versao VARCHAR(30) NOT NULL,
  titulo VARCHAR(200) NOT NULL,
  conteudo LONGTEXT NOT NULL,
  sha256 BINARY(32) NOT NULL,
  publicado_em DATETIME(6) NULL,
  ativo BOOLEAN NOT NULL DEFAULT TRUE,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_documentos_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  UNIQUE KEY uq_documento_versao (tipo, codigo_versao),
  CONSTRAINT ck_documento_tipo CHECK (tipo IN ('TERMOS_ASSOCIACAO','PRIVACIDADE','REGULAMENTO')),
  CONSTRAINT ck_documento_ativo CHECK (ativo IN (0,1))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Texto exato e versionado dos documentos aceitos';

-- 2. PLANOS, CONTRATOS, BENEFICIARIOS E APROVACOES

-- Catalogo geral da rede; excluir oferta significa arquivar
CREATE TABLE IF NOT EXISTS planos (
  id INT NOT NULL AUTO_INCREMENT,
  codigo VARCHAR(30) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  nome VARCHAR(120) NOT NULL,
  descricao TEXT NULL,
  ativo BOOLEAN NOT NULL DEFAULT TRUE,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_planos_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  UNIQUE KEY uq_plano_codigo (codigo),
  CONSTRAINT ck_plano_ativo CHECK (ativo IN (0,1))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Catalogo geral da rede; excluir oferta significa arquivar';

-- Fotografia imutavel de beneficios e cotas depois de publicada
CREATE TABLE IF NOT EXISTS plano_versoes (
  id INT NOT NULL AUTO_INCREMENT,
  plano_id INT NOT NULL,
  numero_versao INT NOT NULL,
  nome VARCHAR(120) NOT NULL,
  descricao TEXT NULL,
  abrangencia VARCHAR(10) NOT NULL,
  duracao_meses INT NOT NULL,
  limite_beneficiarios INT NOT NULL,
  limite_dependentes INT NOT NULL,
  limite_convidados INT NOT NULL,
  carencia_dias INT NOT NULL DEFAULT 0,
  limite_reservas_futuras INT NOT NULL DEFAULT 3,
  beneficios TEXT NOT NULL,
  regras TEXT NOT NULL,
  documento_termos_id INT NOT NULL,
  status VARCHAR(20) NOT NULL DEFAULT 'RASCUNHO',
  publicada_em DATETIME(6) NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_plano_versoes_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  CONSTRAINT fk_pv_plano FOREIGN KEY (plano_id) REFERENCES planos (id),
  CONSTRAINT fk_pv_documento FOREIGN KEY (documento_termos_id) REFERENCES documentos (id),
  UNIQUE KEY uq_pv_numero (plano_id, numero_versao),
  CONSTRAINT ck_pv_numero CHECK (numero_versao > 0),
  CONSTRAINT ck_pv_abrangencia CHECK (abrangencia IN ('LOCAL','REDE')),
  CONSTRAINT ck_pv_duracao CHECK (duracao_meses BETWEEN 1 AND 120),
  CONSTRAINT ck_pv_cotas CHECK (limite_beneficiarios >= 0 AND limite_dependentes BETWEEN 0 AND limite_beneficiarios AND limite_convidados BETWEEN 0 AND limite_beneficiarios),
  CONSTRAINT ck_pv_limites CHECK (carencia_dias >= 0 AND limite_reservas_futuras > 0),
  CONSTRAINT ck_pv_status CHECK (status IN ('RASCUNHO','PUBLICADA','DESATIVADA')),
  CONSTRAINT ck_pv_publicacao CHECK ((status='RASCUNHO' AND publicada_em IS NULL) OR (status IN ('PUBLICADA','DESATIVADA') AND publicada_em IS NOT NULL))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Fotografia imutavel de beneficios e cotas depois de publicada';

-- Preco e parcelamento congelados junto da versao do plano
CREATE TABLE IF NOT EXISTS condicoes_pagamento (
  id INT NOT NULL AUTO_INCREMENT,
  plano_versao_id INT NOT NULL,
  nome VARCHAR(100) NOT NULL,
  meio VARCHAR(20) NOT NULL,
  valor_total_centavos BIGINT NOT NULL,
  quantidade_parcelas INT NOT NULL,
  intervalo_meses INT NOT NULL DEFAULT 1,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_condicoes_pagamento_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  CONSTRAINT fk_condicao_pv FOREIGN KEY (plano_versao_id) REFERENCES plano_versoes (id),
  UNIQUE KEY uq_condicao_nome (plano_versao_id, nome),
  UNIQUE KEY uq_condicao_versao (id, plano_versao_id),
  CONSTRAINT ck_condicao_meio CHECK (meio IN ('PIX','BOLETO','CARTAO','TRANSFERENCIA')),
  CONSTRAINT ck_condicao_valor CHECK (valor_total_centavos BETWEEN 1 AND 999999999999),
  CONSTRAINT ck_condicao_parcelas CHECK (quantidade_parcelas BETWEEN 1 AND 120 AND valor_total_centavos >= quantidade_parcelas AND intervalo_meses BETWEEN 1 AND 12)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Preco e parcelamento congelados junto da versao do plano';

-- Condicoes contratadas preservadas; fim da vigencia exclusivo
CREATE TABLE IF NOT EXISTS contratos (
  id INT NOT NULL AUTO_INCREMENT,
  numero VARCHAR(40) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  titular_pessoa_id INT NOT NULL,
  clube_origem_id INT NOT NULL,
  plano_versao_id INT NOT NULL,
  condicao_pagamento_id INT NOT NULL,
  contrato_anterior_id INT NULL,
  data_inicio DATE NOT NULL,
  data_fim DATE NOT NULL,
  proposta_expira_em DATETIME(6) NULL,
  valor_total_centavos BIGINT NOT NULL,
  quantidade_parcelas INT NOT NULL,
  dia_vencimento INT NOT NULL,
  moeda CHAR(3) CHARACTER SET ascii COLLATE ascii_bin NOT NULL DEFAULT 'BRL',
  status VARCHAR(20) NOT NULL DEFAULT 'PENDENTE',
  ativado_em DATETIME(6) NULL,
  suspenso_em DATETIME(6) NULL,
  motivo_suspensao VARCHAR(500) NULL,
  cancelado_em DATETIME(6) NULL,
  motivo_cancelamento VARCHAR(500) NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_contratos_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  UNIQUE KEY uq_contrato_numero (numero),
  UNIQUE KEY uq_contrato_titular (id, titular_pessoa_id),
  CONSTRAINT fk_contrato_titular FOREIGN KEY (titular_pessoa_id) REFERENCES pessoas (id),
  CONSTRAINT fk_contrato_clube FOREIGN KEY (clube_origem_id) REFERENCES clubes (id),
  CONSTRAINT fk_contrato_condicao FOREIGN KEY (condicao_pagamento_id, plano_versao_id) REFERENCES condicoes_pagamento (id, plano_versao_id),
  CONSTRAINT fk_contrato_anterior FOREIGN KEY (contrato_anterior_id) REFERENCES contratos (id),
  KEY ix_contrato_clube_status (clube_origem_id, status),
  KEY ix_contrato_titular_status (titular_pessoa_id, status),
  CONSTRAINT ck_contrato_datas CHECK (data_fim > data_inicio),
  CONSTRAINT ck_contrato_valor CHECK (valor_total_centavos BETWEEN 1 AND 999999999999),
  CONSTRAINT ck_contrato_parcelas CHECK (quantidade_parcelas BETWEEN 1 AND 120 AND valor_total_centavos >= quantidade_parcelas AND dia_vencimento BETWEEN 1 AND 28),
  CONSTRAINT ck_contrato_moeda CHECK (moeda='BRL'),
  CONSTRAINT ck_contrato_status CHECK (status IN ('PENDENTE','VIGENTE','SUSPENSO','CANCELADO','ENCERRADO'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Condicoes contratadas preservadas; fim da vigencia exclusivo';

-- Titular fundador e beneficiarios do contrato
CREATE TABLE IF NOT EXISTS membros_contrato (
  id INT NOT NULL AUTO_INCREMENT,
  contrato_id INT NOT NULL,
  pessoa_id INT NOT NULL,
  tipo VARCHAR(15) NOT NULL,
  parentesco VARCHAR(60) NULL,
  responsavel_pessoa_id INT NULL,
  status VARCHAR(20) NOT NULL DEFAULT 'PENDENTE',
  aprovado_por_usuario_id INT NULL,
  aprovado_em DATETIME(6) NULL,
  removido_em DATETIME(6) NULL,
  motivo_status VARCHAR(500) NULL,
  titular_unico INT GENERATED ALWAYS AS (CASE WHEN tipo='TITULAR' THEN contrato_id ELSE NULL END) STORED,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_membros_contrato_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  CONSTRAINT fk_membro_contrato FOREIGN KEY (contrato_id) REFERENCES contratos (id),
  CONSTRAINT fk_membro_pessoa FOREIGN KEY (pessoa_id) REFERENCES pessoas (id),
  CONSTRAINT fk_membro_responsavel FOREIGN KEY (responsavel_pessoa_id) REFERENCES pessoas (id),
  CONSTRAINT fk_membro_aprovador FOREIGN KEY (aprovado_por_usuario_id) REFERENCES usuarios (id),
  UNIQUE KEY uq_membro_pessoa (contrato_id, pessoa_id),
  UNIQUE KEY uq_membro_titular (titular_unico),
  UNIQUE KEY uq_membro_identidade (id, pessoa_id),
  KEY ix_membro_pessoa_status (pessoa_id, status),
  KEY ix_membro_cota (contrato_id, status, tipo),
  CONSTRAINT ck_membro_tipo CHECK (tipo IN ('TITULAR','DEPENDENTE','CONVIDADO')),
  CONSTRAINT ck_membro_status CHECK (status IN ('PENDENTE','ATIVO','REJEITADO','SUSPENSO','REMOVIDO'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Titular fundador e beneficiarios do contrato';

-- Historico de revisao dos convidados e dependentes
CREATE TABLE IF NOT EXISTS solicitacoes_vinculo (
  id INT NOT NULL AUTO_INCREMENT,
  protocolo VARCHAR(40) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  membro_contrato_id INT NOT NULL,
  solicitante_usuario_id INT NOT NULL,
  tipo VARCHAR(15) NOT NULL,
  dados_propostos JSON NOT NULL,
  arquivo_id INT NULL,
  status VARCHAR(15) NOT NULL DEFAULT 'PENDENTE',
  analisado_por_usuario_id INT NULL,
  analisado_em DATETIME(6) NULL,
  justificativa VARCHAR(1000) NULL,
  pendente_unico INT GENERATED ALWAYS AS (CASE WHEN status='PENDENTE' THEN membro_contrato_id ELSE NULL END) STORED,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_solicitacoes_vinculo_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  UNIQUE KEY uq_sv_protocolo (protocolo),
  UNIQUE KEY uq_sv_pendente (pendente_unico),
  CONSTRAINT fk_sv_membro FOREIGN KEY (membro_contrato_id) REFERENCES membros_contrato (id),
  CONSTRAINT fk_sv_autor FOREIGN KEY (solicitante_usuario_id) REFERENCES usuarios (id),
  CONSTRAINT fk_sv_arquivo FOREIGN KEY (arquivo_id) REFERENCES arquivos (id),
  CONSTRAINT fk_sv_analista FOREIGN KEY (analisado_por_usuario_id) REFERENCES usuarios (id),
  KEY ix_sv_status (status, created_at),
  CONSTRAINT ck_sv_tipo CHECK (tipo IN ('INCLUSAO','ALTERACAO','REMOCAO')),
  CONSTRAINT ck_sv_status CHECK (status IN ('PENDENTE','APROVADA','REJEITADA','CANCELADA')),
  CONSTRAINT ck_sv_recusa CHECK (status <> 'REJEITADA' OR (justificativa IS NOT NULL AND CHAR_LENGTH(TRIM(justificativa)) > 0))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Historico de revisao dos convidados e dependentes';

-- Evidencia do texto e da versao aceitos
CREATE TABLE IF NOT EXISTS aceites_documentos (
  id INT NOT NULL AUTO_INCREMENT,
  documento_id INT NOT NULL,
  pessoa_id INT NOT NULL,
  registrado_por_usuario_id INT NOT NULL,
  contrato_id INT NULL,
  manifestacao VARCHAR(10) NOT NULL,
  canal VARCHAR(10) NOT NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (id),
  CONSTRAINT fk_aceite_documento FOREIGN KEY (documento_id) REFERENCES documentos (id),
  CONSTRAINT fk_aceite_pessoa FOREIGN KEY (pessoa_id) REFERENCES pessoas (id),
  CONSTRAINT fk_aceite_autor FOREIGN KEY (registrado_por_usuario_id) REFERENCES usuarios (id),
  CONSTRAINT fk_aceite_contrato FOREIGN KEY (contrato_id) REFERENCES contratos (id),
  KEY ix_aceite_pessoa (pessoa_id, documento_id),
  CONSTRAINT ck_aceite_tipo CHECK (manifestacao IN ('ACEITE','CIENCIA')),
  CONSTRAINT ck_aceite_canal CHECK (canal IN ('MOBILE','WEB'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Evidencia do texto e da versao aceitos';

-- 3. FATURAS, RECEBIMENTOS E HISTORICO FINANCEIRO

-- Parcelas; atraso calculado pela API na data civil da unidade
CREATE TABLE IF NOT EXISTS faturas (
  id INT NOT NULL AUTO_INCREMENT,
  contrato_id INT NOT NULL,
  numero VARCHAR(40) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  numero_parcela INT NOT NULL,
  descricao VARCHAR(200) NOT NULL,
  vencimento DATE NOT NULL,
  valor_centavos BIGINT NOT NULL,
  status VARCHAR(15) NOT NULL DEFAULT 'ABERTA',
  quitada_em DATETIME(6) NULL,
  cancelada_em DATETIME(6) NULL,
  cancelada_por_usuario_id INT NULL,
  motivo_cancelamento VARCHAR(500) NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_faturas_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  CONSTRAINT fk_fatura_contrato FOREIGN KEY (contrato_id) REFERENCES contratos (id),
  CONSTRAINT fk_fatura_cancelador FOREIGN KEY (cancelada_por_usuario_id) REFERENCES usuarios (id),
  UNIQUE KEY uq_fatura_numero (numero),
  UNIQUE KEY uq_fatura_parcela (contrato_id, numero_parcela),
  KEY ix_fatura_vencimento (status, vencimento),
  CONSTRAINT ck_fatura_parcela CHECK (numero_parcela BETWEEN 1 AND 120),
  CONSTRAINT ck_fatura_valor CHECK (valor_centavos BETWEEN 1 AND 999999999999),
  CONSTRAINT ck_fatura_status CHECK (status IN ('ABERTA','QUITADA','CANCELADA')),
  CONSTRAINT ck_fatura_quitacao CHECK ((status='QUITADA' AND quitada_em IS NOT NULL) OR (status<>'QUITADA' AND quitada_em IS NULL))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Parcelas; atraso calculado pela API na data civil da unidade';

-- Recebimentos conferidos; sem cobranca bancaria nesta etapa
CREATE TABLE IF NOT EXISTS pagamentos (
  id INT NOT NULL AUTO_INCREMENT,
  fatura_id INT NOT NULL,
  origem VARCHAR(10) NOT NULL DEFAULT 'MANUAL',
  meio VARCHAR(20) NOT NULL,
  valor_centavos BIGINT NOT NULL,
  moeda CHAR(3) CHARACTER SET ascii COLLATE ascii_bin NOT NULL DEFAULT 'BRL',
  status VARCHAR(15) NOT NULL DEFAULT 'PENDENTE',
  quitacao_aplicada BOOLEAN NOT NULL DEFAULT FALSE,
  conciliacao VARCHAR(15) NOT NULL DEFAULT 'PENDENTE',
  provedor VARCHAR(60) CHARACTER SET ascii COLLATE ascii_bin NULL,
  referencia_externa VARCHAR(150) CHARACTER SET ascii COLLATE ascii_bin NULL,
  referencia_manual VARCHAR(200) NULL,
  comprovante_arquivo_id INT NULL,
  registrado_por_usuario_id INT NULL,
  confirmado_por_usuario_id INT NULL,
  pago_em DATETIME(6) NULL,
  confirmado_em DATETIME(6) NULL,
  observacao VARCHAR(1000) NULL,
  fatura_quitada_unica INT GENERATED ALWAYS AS (CASE WHEN quitacao_aplicada=1 THEN fatura_id ELSE NULL END) STORED,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_pagamentos_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  CONSTRAINT fk_pag_fatura FOREIGN KEY (fatura_id) REFERENCES faturas (id),
  CONSTRAINT fk_pag_arquivo FOREIGN KEY (comprovante_arquivo_id) REFERENCES arquivos (id),
  CONSTRAINT fk_pag_autor FOREIGN KEY (registrado_por_usuario_id) REFERENCES usuarios (id),
  CONSTRAINT fk_pag_confirmador FOREIGN KEY (confirmado_por_usuario_id) REFERENCES usuarios (id),
  UNIQUE KEY uq_pag_provedor (provedor, referencia_externa),
  UNIQUE KEY uq_pag_quitacao (fatura_quitada_unica),
  CONSTRAINT ck_pag_origem CHECK (origem IN ('MANUAL','PROVEDOR')),
  CONSTRAINT ck_pag_meio CHECK (meio IN ('PIX','BOLETO','CARTAO','TRANSFERENCIA','DINHEIRO','OUTRO')),
  CONSTRAINT ck_pag_valor CHECK (valor_centavos BETWEEN 1 AND 999999999999),
  CONSTRAINT ck_pag_moeda CHECK (moeda='BRL'),
  CONSTRAINT ck_pag_status CHECK (status IN ('PENDENTE','EM_ANALISE','CONFIRMADO','REJEITADO','CANCELADO')),
  CONSTRAINT ck_pag_conciliacao CHECK (conciliacao IN ('PENDENTE','REGULAR','DIVERGENTE')),
  CONSTRAINT ck_pag_quitacao CHECK (quitacao_aplicada IN (0,1) AND (quitacao_aplicada=0 OR (status='CONFIRMADO' AND pago_em IS NOT NULL AND confirmado_em IS NOT NULL))),
  CONSTRAINT ck_pag_provedor CHECK (origem<>'PROVEDOR' OR (provedor IS NOT NULL AND referencia_externa IS NOT NULL))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Recebimentos conferidos; sem cobranca bancaria nesta etapa';

-- Estrutura reservada para futuro estorno integral; sem endpoint atual
CREATE TABLE IF NOT EXISTS estornos (
  id INT NOT NULL AUTO_INCREMENT,
  pagamento_id INT NOT NULL,
  valor_centavos BIGINT NOT NULL,
  motivo VARCHAR(500) NOT NULL,
  status VARCHAR(15) NOT NULL DEFAULT 'SOLICITADO',
  solicitado_por_usuario_id INT NOT NULL,
  confirmado_por_usuario_id INT NULL,
  confirmado_em DATETIME(6) NULL,
  provedor VARCHAR(60) CHARACTER SET ascii COLLATE ascii_bin NULL,
  referencia_externa VARCHAR(150) CHARACTER SET ascii COLLATE ascii_bin NULL,
  pagamento_estornado_unico INT GENERATED ALWAYS AS (CASE WHEN status='CONFIRMADO' THEN pagamento_id ELSE NULL END) STORED,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_estornos_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  CONSTRAINT fk_estorno_pag FOREIGN KEY (pagamento_id) REFERENCES pagamentos (id),
  CONSTRAINT fk_estorno_autor FOREIGN KEY (solicitado_por_usuario_id) REFERENCES usuarios (id),
  CONSTRAINT fk_estorno_confirmador FOREIGN KEY (confirmado_por_usuario_id) REFERENCES usuarios (id),
  UNIQUE KEY uq_estorno_pagamento (pagamento_estornado_unico),
  UNIQUE KEY uq_estorno_provedor (provedor, referencia_externa),
  CONSTRAINT ck_estorno_valor CHECK (valor_centavos BETWEEN 1 AND 999999999999),
  CONSTRAINT ck_estorno_status CHECK (status IN ('SOLICITADO','CONFIRMADO','REJEITADO'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Estrutura reservada para futuro estorno integral; sem endpoint atual';

-- Reserva de evolucao para webhooks; nenhum provedor configurado
CREATE TABLE IF NOT EXISTS eventos_provedor (
  id INT NOT NULL AUTO_INCREMENT,
  provedor VARCHAR(60) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  evento_externo_id VARCHAR(150) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  tipo VARCHAR(100) NOT NULL,
  pagamento_id INT NULL,
  payload_hash BINARY(32) NOT NULL,
  dados JSON NOT NULL,
  status VARCHAR(15) NOT NULL DEFAULT 'RECEBIDO',
  tentativas INT NOT NULL DEFAULT 0,
  processado_em DATETIME(6) NULL,
  proxima_tentativa_em DATETIME(6) NULL,
  erro VARCHAR(1000) NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_eventos_provedor_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  CONSTRAINT fk_webhook_pag FOREIGN KEY (pagamento_id) REFERENCES pagamentos (id),
  UNIQUE KEY uq_webhook_evento (provedor, evento_externo_id),
  CONSTRAINT ck_webhook_status CHECK (status IN ('RECEBIDO','PROCESSADO','DIVERGENTE','ERRO')),
  CONSTRAINT ck_webhook_tentativas CHECK (tentativas >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Reserva de evolucao para webhooks; nenhum provedor configurado';

-- 4. VISITANTES, AREAS, EVENTOS E AGENDA COMPARTILHADA

-- Autorizacao para visita em um dia civil especifico
CREATE TABLE IF NOT EXISTS visitantes (
  id INT NOT NULL AUTO_INCREMENT,
  membro_responsavel_id INT NOT NULL,
  clube_id INT NOT NULL,
  cadastrado_por_usuario_id INT NOT NULL,
  nome VARCHAR(160) NOT NULL,
  tipo_documento VARCHAR(30) NOT NULL,
  documento VARCHAR(40) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
  nascimento DATE NOT NULL,
  telefone VARCHAR(25) NULL,
  data_visita DATE NOT NULL,
  placa VARCHAR(12) NULL,
  observacao VARCHAR(1000) NULL,
  status VARCHAR(15) NOT NULL DEFAULT 'PREVISTO',
  entrada_em DATETIME(6) NULL,
  saida_em DATETIME(6) NULL,
  cancelado_em DATETIME(6) NULL,
  cancelado_por_usuario_id INT NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_visitantes_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  CONSTRAINT fk_vis_responsavel FOREIGN KEY (membro_responsavel_id) REFERENCES membros_contrato (id),
  CONSTRAINT fk_vis_clube FOREIGN KEY (clube_id) REFERENCES clubes (id),
  CONSTRAINT fk_vis_autor FOREIGN KEY (cadastrado_por_usuario_id) REFERENCES usuarios (id),
  CONSTRAINT fk_vis_cancelador FOREIGN KEY (cancelado_por_usuario_id) REFERENCES usuarios (id),
  KEY ix_vis_unidade_data (clube_id, data_visita, status),
  KEY ix_vis_responsavel_data (membro_responsavel_id, data_visita),
  CONSTRAINT ck_vis_status CHECK (status IN ('PREVISTO','PRESENTE','SAIU','CANCELADO')),
  CONSTRAINT ck_vis_horarios CHECK (saida_em IS NULL OR (entrada_em IS NOT NULL AND saida_em >= entrada_em))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Autorizacao para visita em um dia civil especifico';

-- Limite padrao: 180 minutos para esportes e jogos; NULL para saloes
CREATE TABLE IF NOT EXISTS tipos_area (
  id INT NOT NULL AUTO_INCREMENT,
  codigo VARCHAR(30) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  nome VARCHAR(100) NOT NULL,
  max_minutos INT NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_tipos_area_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  UNIQUE KEY uq_tipo_area_codigo (codigo),
  CONSTRAINT ck_tipo_area_duracao CHECK (max_minutos IS NULL OR max_minutos BETWEEN 1 AND 180)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Limite padrao: 180 minutos para esportes e jogos; NULL para saloes';

-- Recurso reservavel de uma unidade
CREATE TABLE IF NOT EXISTS areas (
  id INT NOT NULL AUTO_INCREMENT,
  clube_id INT NOT NULL,
  tipo_area_id INT NOT NULL,
  nome VARCHAR(120) NOT NULL,
  descricao TEXT NOT NULL,
  regras TEXT NOT NULL,
  imagem VARCHAR(500) NULL,
  capacidade INT NOT NULL,
  max_minutos INT NULL,
  antecedencia_minima_horas INT NOT NULL DEFAULT 0,
  antecedencia_maxima_dias INT NOT NULL DEFAULT 90,
  intervalo_apos_minutos INT NOT NULL DEFAULT 0,
  cancelamento_minimo_horas INT NOT NULL DEFAULT 24,
  ativo BOOLEAN NOT NULL DEFAULT TRUE,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_areas_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  CONSTRAINT fk_area_clube FOREIGN KEY (clube_id) REFERENCES clubes (id),
  CONSTRAINT fk_area_tipo FOREIGN KEY (tipo_area_id) REFERENCES tipos_area (id),
  UNIQUE KEY uq_area_nome (clube_id, nome),
  UNIQUE KEY uq_area_unidade (id, clube_id),
  CONSTRAINT ck_area_capacidade CHECK (capacidade > 0),
  CONSTRAINT ck_area_duracao CHECK (max_minutos IS NULL OR max_minutos > 0),
  CONSTRAINT ck_area_limites CHECK (antecedencia_minima_horas >= 0 AND antecedencia_maxima_dias > 0 AND intervalo_apos_minutos >= 0 AND cancelamento_minimo_horas >= 0),
  CONSTRAINT ck_area_ativo CHECK (ativo IN (0,1))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Recurso reservavel de uma unidade';

-- Horario local; 0 domingo, 6 sabado; dividir faixas que cruzam meia-noite
CREATE TABLE IF NOT EXISTS horarios_area (
  id INT NOT NULL AUTO_INCREMENT,
  area_id INT NOT NULL,
  dia_semana INT NOT NULL,
  inicio TIME NOT NULL,
  fim TIME NOT NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_horarios_area_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  CONSTRAINT fk_ha_area FOREIGN KEY (area_id) REFERENCES areas (id),
  UNIQUE KEY uq_ha_faixa (area_id, dia_semana, inicio, fim),
  CONSTRAINT ck_ha_dia CHECK (dia_semana BETWEEN 0 AND 6),
  CONSTRAINT ck_ha_intervalo CHECK (inicio >= '00:00:00' AND fim <= '24:00:00' AND fim > inicio)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Horario local; 0 domingo, 6 sabado; dividir faixas que cruzam meia-noite';

-- Evento do clube; API projeta Realizado depois do fim
CREATE TABLE IF NOT EXISTS eventos (
  id INT NOT NULL AUTO_INCREMENT,
  clube_id INT NOT NULL,
  criado_por_usuario_id INT NOT NULL,
  titulo VARCHAR(200) NOT NULL,
  descricao TEXT NOT NULL,
  inicio_em DATETIME(6) NOT NULL,
  fim_em DATETIME(6) NOT NULL,
  publico VARCHAR(15) NOT NULL,
  capacidade INT NULL,
  local_descricao VARCHAR(200) NOT NULL,
  imagem VARCHAR(500) NULL,
  inscricoes_abrem_em DATETIME(6) NULL,
  inscricoes_fecham_em DATETIME(6) NULL,
  status VARCHAR(15) NOT NULL DEFAULT 'AGENDADO',
  cancelado_em DATETIME(6) NULL,
  motivo_cancelamento VARCHAR(500) NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_eventos_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  CONSTRAINT fk_evento_clube FOREIGN KEY (clube_id) REFERENCES clubes (id),
  CONSTRAINT fk_evento_autor FOREIGN KEY (criado_por_usuario_id) REFERENCES usuarios (id),
  UNIQUE KEY uq_evento_unidade (id, clube_id),
  KEY ix_evento_unidade_inicio (clube_id, inicio_em),
  CONSTRAINT ck_evento_intervalo CHECK (fim_em > inicio_em),
  CONSTRAINT ck_evento_publico CHECK (publico IN ('ASSOCIADOS','PUBLICO')),
  CONSTRAINT ck_evento_capacidade CHECK (capacidade IS NULL OR capacidade > 0),
  CONSTRAINT ck_evento_inscricoes CHECK ((inscricoes_abrem_em IS NULL AND inscricoes_fecham_em IS NULL) OR (inscricoes_abrem_em IS NOT NULL AND inscricoes_fecham_em IS NOT NULL AND inscricoes_fecham_em > inscricoes_abrem_em AND inscricoes_fecham_em <= inicio_em)),
  CONSTRAINT ck_evento_status CHECK (status IN ('AGENDADO','CONFIRMADO','CANCELADO'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Evento do clube; API projeta Realizado depois do fim';

-- Estrutura para inscricoes futuras; Mobile atual somente consulta eventos
CREATE TABLE IF NOT EXISTS inscricoes_evento (
  id INT NOT NULL AUTO_INCREMENT,
  evento_id INT NOT NULL,
  pessoa_id INT NOT NULL,
  registrado_por_usuario_id INT NOT NULL,
  status VARCHAR(15) NOT NULL DEFAULT 'CONFIRMADA',
  cancelada_em DATETIME(6) NULL,
  presenca_em DATETIME(6) NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_inscricoes_evento_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  CONSTRAINT fk_ie_evento FOREIGN KEY (evento_id) REFERENCES eventos (id),
  CONSTRAINT fk_ie_pessoa FOREIGN KEY (pessoa_id) REFERENCES pessoas (id),
  CONSTRAINT fk_ie_autor FOREIGN KEY (registrado_por_usuario_id) REFERENCES usuarios (id),
  UNIQUE KEY uq_ie_pessoa (evento_id, pessoa_id),
  CONSTRAINT ck_ie_status CHECK (status IN ('CONFIRMADA','CANCELADA','PRESENTE','AUSENTE'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Estrutura para inscricoes futuras; Mobile atual somente consulta eventos';

-- Reserva do associado; horario e area ficam na ocupacao
CREATE TABLE IF NOT EXISTS reservas (
  id INT NOT NULL AUTO_INCREMENT,
  protocolo VARCHAR(40) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  membro_contrato_id INT NOT NULL,
  criado_por_usuario_id INT NOT NULL,
  finalidade VARCHAR(200) NOT NULL,
  quantidade_pessoas INT NOT NULL DEFAULT 1,
  regras_aceitas TEXT NOT NULL,
  cancelamento_minimo_horas INT NOT NULL,
  status VARCHAR(15) NOT NULL DEFAULT 'CONFIRMADA',
  cancelado_por_usuario_id INT NULL,
  cancelado_em DATETIME(6) NULL,
  motivo_cancelamento VARCHAR(500) NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_reservas_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  CONSTRAINT fk_reserva_membro FOREIGN KEY (membro_contrato_id) REFERENCES membros_contrato (id),
  CONSTRAINT fk_reserva_autor FOREIGN KEY (criado_por_usuario_id) REFERENCES usuarios (id),
  CONSTRAINT fk_reserva_cancelador FOREIGN KEY (cancelado_por_usuario_id) REFERENCES usuarios (id),
  UNIQUE KEY uq_reserva_protocolo (protocolo),
  KEY ix_reserva_membro_status (membro_contrato_id, status),
  CONSTRAINT ck_reserva_pessoas CHECK (quantidade_pessoas > 0),
  CONSTRAINT ck_reserva_cancelamento CHECK (cancelamento_minimo_horas >= 0),
  CONSTRAINT ck_reserva_status CHECK (status IN ('CONFIRMADA','CANCELADA','CONCLUIDA'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Reserva do associado; horario e area ficam na ocupacao';

-- Agenda unica de reservas, eventos e bloqueios; ver protocolo transacional
CREATE TABLE IF NOT EXISTS ocupacoes_area (
  id INT NOT NULL AUTO_INCREMENT,
  area_id INT NOT NULL,
  clube_id INT NOT NULL,
  tipo VARCHAR(10) NOT NULL,
  reserva_id INT NULL,
  evento_id INT NULL,
  inicio_em DATETIME(6) NOT NULL,
  fim_em DATETIME(6) NOT NULL,
  intervalo_apos_minutos INT NOT NULL DEFAULT 0,
  status VARCHAR(10) NOT NULL DEFAULT 'ATIVA',
  criado_por_usuario_id INT NOT NULL,
  motivo VARCHAR(500) NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_ocupacoes_area_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  CONSTRAINT fk_ocup_area_unidade FOREIGN KEY (area_id, clube_id) REFERENCES areas (id, clube_id),
  CONSTRAINT fk_ocup_evento_unidade FOREIGN KEY (evento_id, clube_id) REFERENCES eventos (id, clube_id),
  CONSTRAINT fk_ocup_reserva FOREIGN KEY (reserva_id) REFERENCES reservas (id),
  CONSTRAINT fk_ocup_autor FOREIGN KEY (criado_por_usuario_id) REFERENCES usuarios (id),
  UNIQUE KEY uq_ocup_reserva (reserva_id),
  UNIQUE KEY uq_ocup_evento_area (evento_id, area_id),
  KEY ix_ocup_agenda (area_id, status, inicio_em, fim_em),
  CONSTRAINT ck_ocup_intervalo CHECK (fim_em > inicio_em AND intervalo_apos_minutos >= 0),
  CONSTRAINT ck_ocup_status CHECK (status IN ('ATIVA','CANCELADA')),
  CONSTRAINT ck_ocup_origem CHECK ((tipo='RESERVA' AND reserva_id IS NOT NULL AND evento_id IS NULL) OR (tipo='EVENTO' AND evento_id IS NOT NULL AND reserva_id IS NULL) OR (tipo='BLOQUEIO' AND reserva_id IS NULL AND evento_id IS NULL))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Agenda unica de reservas, eventos e bloqueios; ver protocolo transacional';

-- 5. SOLICITACOES, RECLAMACOES E CHAT

-- Atendimentos privados; reclamacao oculta status apenas no Mobile
CREATE TABLE IF NOT EXISTS chamados (
  id INT NOT NULL AUTO_INCREMENT,
  protocolo VARCHAR(40) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  clube_id INT NOT NULL,
  autor_usuario_id INT NOT NULL,
  contrato_id INT NULL,
  tipo VARCHAR(15) NOT NULL,
  motivo VARCHAR(200) NOT NULL,
  prioridade VARCHAR(10) NOT NULL DEFAULT 'NORMAL',
  status VARCHAR(25) NOT NULL DEFAULT 'ABERTO',
  responsavel_usuario_id INT NULL,
  resolvido_em DATETIME(6) NULL,
  fechado_em DATETIME(6) NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_chamados_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  UNIQUE KEY uq_chamado_protocolo (protocolo),
  CONSTRAINT fk_chamado_clube FOREIGN KEY (clube_id) REFERENCES clubes (id),
  CONSTRAINT fk_chamado_autor FOREIGN KEY (autor_usuario_id) REFERENCES usuarios (id),
  CONSTRAINT fk_chamado_contrato FOREIGN KEY (contrato_id) REFERENCES contratos (id),
  CONSTRAINT fk_chamado_responsavel FOREIGN KEY (responsavel_usuario_id) REFERENCES usuarios (id),
  KEY ix_chamado_fila (clube_id, tipo, status, created_at),
  KEY ix_chamado_autor (autor_usuario_id, tipo, created_at),
  CONSTRAINT ck_chamado_tipo CHECK (tipo IN ('SOLICITACAO','RECLAMACAO')),
  CONSTRAINT ck_chamado_prioridade CHECK (prioridade IN ('NORMAL','ALTA')),
  CONSTRAINT ck_chamado_status CHECK (status IN ('ABERTO','EM_ATENDIMENTO','AGUARDANDO_ASSOCIADO','RESOLVIDO','FECHADO'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Atendimentos privados; reclamacao oculta status apenas no Mobile';

-- Primeira mensagem e todas as respostas, sem sobrescrever historico
CREATE TABLE IF NOT EXISTS mensagens_chamado (
  id INT NOT NULL AUTO_INCREMENT,
  chamado_id INT NOT NULL,
  autor_usuario_id INT NOT NULL,
  autor_nome VARCHAR(160) NOT NULL,
  autor_perfil VARCHAR(20) NOT NULL,
  texto TEXT NOT NULL,
  arquivo_id INT NULL,
  chave_cliente VARCHAR(100) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (id),
  CONSTRAINT fk_mc_chamado FOREIGN KEY (chamado_id) REFERENCES chamados (id),
  CONSTRAINT fk_mc_autor FOREIGN KEY (autor_usuario_id) REFERENCES usuarios (id),
  CONSTRAINT fk_mc_arquivo FOREIGN KEY (arquivo_id) REFERENCES arquivos (id),
  UNIQUE KEY uq_mc_chave (autor_usuario_id, chave_cliente),
  KEY ix_mc_historico (chamado_id, id),
  CONSTRAINT ck_mc_texto CHECK (CHAR_LENGTH(TRIM(texto)) BETWEEN 1 AND 10000),
  CONSTRAINT ck_mc_perfil CHECK (autor_perfil IN ('ASSOCIADO','ADM_LOCAL','ADM_GERAL'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Primeira mensagem e todas as respostas, sem sobrescrever historico';

-- Grupo geral por unidade; autoria determinada no servidor
CREATE TABLE IF NOT EXISTS mensagens_chat (
  id INT NOT NULL AUTO_INCREMENT,
  clube_id INT NOT NULL,
  autor_usuario_id INT NOT NULL,
  autor_nome VARCHAR(160) NOT NULL,
  autor_perfil VARCHAR(20) NOT NULL,
  texto TEXT NOT NULL,
  chave_cliente VARCHAR(100) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  fixada BOOLEAN NOT NULL DEFAULT FALSE,
  status VARCHAR(10) NOT NULL DEFAULT 'VISIVEL',
  moderado_por_usuario_id INT NULL,
  moderado_em DATETIME(6) NULL,
  motivo_moderacao VARCHAR(500) NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_mensagens_chat_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  CONSTRAINT fk_chat_clube FOREIGN KEY (clube_id) REFERENCES clubes (id),
  CONSTRAINT fk_chat_autor FOREIGN KEY (autor_usuario_id) REFERENCES usuarios (id),
  CONSTRAINT fk_chat_moderador FOREIGN KEY (moderado_por_usuario_id) REFERENCES usuarios (id),
  UNIQUE KEY uq_chat_chave (autor_usuario_id, chave_cliente),
  UNIQUE KEY uq_chat_unidade (id, clube_id),
  KEY ix_chat_pagina (clube_id, status, id),
  CONSTRAINT ck_chat_texto CHECK (CHAR_LENGTH(TRIM(texto)) BETWEEN 1 AND 2000),
  CONSTRAINT ck_chat_perfil CHECK (autor_perfil IN ('ASSOCIADO','ADM_LOCAL','ADM_GERAL')),
  CONSTRAINT ck_chat_fixada CHECK (fixada IN (0,1)),
  CONSTRAINT ck_chat_status CHECK (status IN ('VISIVEL','OCULTA'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Grupo geral por unidade; autoria determinada no servidor';

-- Ultima leitura de cada participante por unidade
CREATE TABLE IF NOT EXISTS leituras_chat (
  clube_id INT NOT NULL,
  usuario_id INT NOT NULL,
  ultima_mensagem_id INT NULL,
  lida_em DATETIME(6) NOT NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_leituras_chat_versao CHECK (versao >= 1),
  PRIMARY KEY (clube_id, usuario_id),
  CONSTRAINT fk_leitura_clube FOREIGN KEY (clube_id) REFERENCES clubes (id),
  CONSTRAINT fk_leitura_usuario FOREIGN KEY (usuario_id) REFERENCES usuarios (id),
  CONSTRAINT fk_leitura_mensagem_unidade FOREIGN KEY (ultima_mensagem_id, clube_id) REFERENCES mensagens_chat (id, clube_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Ultima leitura de cada participante por unidade';

-- Restricoes temporarias de participacao no chat
CREATE TABLE IF NOT EXISTS moderacoes_chat (
  id INT NOT NULL AUTO_INCREMENT,
  clube_id INT NOT NULL,
  usuario_id INT NOT NULL,
  aplicada_por_usuario_id INT NOT NULL,
  motivo VARCHAR(500) NOT NULL,
  inicio_em DATETIME(6) NOT NULL,
  fim_em DATETIME(6) NULL,
  revogada_em DATETIME(6) NULL,
  revogada_por_usuario_id INT NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_moderacoes_chat_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  CONSTRAINT fk_mod_clube FOREIGN KEY (clube_id) REFERENCES clubes (id),
  CONSTRAINT fk_mod_usuario FOREIGN KEY (usuario_id) REFERENCES usuarios (id),
  CONSTRAINT fk_mod_autor FOREIGN KEY (aplicada_por_usuario_id) REFERENCES usuarios (id),
  CONSTRAINT fk_mod_revogador FOREIGN KEY (revogada_por_usuario_id) REFERENCES usuarios (id),
  KEY ix_mod_usuario_clube (usuario_id, clube_id, revogada_em),
  CONSTRAINT ck_mod_intervalo CHECK (fim_em IS NULL OR fim_em > inicio_em)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Restricoes temporarias de participacao no chat';

-- Historico de denuncias de mensagens
CREATE TABLE IF NOT EXISTS denuncias_chat (
  id INT NOT NULL AUTO_INCREMENT,
  mensagem_id INT NOT NULL,
  denunciante_usuario_id INT NOT NULL,
  motivo VARCHAR(500) NOT NULL,
  status VARCHAR(15) NOT NULL DEFAULT 'ABERTA',
  analisada_por_usuario_id INT NULL,
  analisada_em DATETIME(6) NULL,
  resposta VARCHAR(1000) NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_denuncias_chat_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  CONSTRAINT fk_den_mensagem FOREIGN KEY (mensagem_id) REFERENCES mensagens_chat (id),
  CONSTRAINT fk_den_autor FOREIGN KEY (denunciante_usuario_id) REFERENCES usuarios (id),
  CONSTRAINT fk_den_analista FOREIGN KEY (analisada_por_usuario_id) REFERENCES usuarios (id),
  UNIQUE KEY uq_den_pessoa (mensagem_id, denunciante_usuario_id),
  CONSTRAINT ck_den_status CHECK (status IN ('ABERTA','PROCEDENTE','IMPROCEDENTE'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Historico de denuncias de mensagens';

-- 6. VOTACOES, ELEITORADO E VOTOS

-- Eleicao da unidade; votante elegivel e titular da unidade de origem
CREATE TABLE IF NOT EXISTS votacoes (
  id INT NOT NULL AUTO_INCREMENT,
  clube_id INT NOT NULL,
  criada_por_usuario_id INT NOT NULL,
  motivo VARCHAR(200) NOT NULL,
  descricao TEXT NOT NULL,
  regras TEXT NOT NULL,
  inicio_em DATETIME(6) NOT NULL,
  fim_em DATETIME(6) NOT NULL,
  exige_adimplencia BOOLEAN NOT NULL DEFAULT FALSE,
  quorum_percentual DECIMAL(5,2) NOT NULL DEFAULT 0,
  status VARCHAR(15) NOT NULL DEFAULT 'RASCUNHO',
  publicada_em DATETIME(6) NULL,
  eleitorado_gerado_em DATETIME(6) NULL,
  encerrada_em DATETIME(6) NULL,
  motivo_cancelamento VARCHAR(500) NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_votacoes_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  CONSTRAINT fk_votacao_clube FOREIGN KEY (clube_id) REFERENCES clubes (id),
  CONSTRAINT fk_votacao_autor FOREIGN KEY (criada_por_usuario_id) REFERENCES usuarios (id),
  KEY ix_votacao_unidade (clube_id, inicio_em),
  CONSTRAINT ck_votacao_intervalo CHECK (fim_em > inicio_em),
  CONSTRAINT ck_votacao_quorum CHECK (quorum_percentual BETWEEN 0 AND 100),
  CONSTRAINT ck_votacao_adimplencia CHECK (exige_adimplencia IN (0,1)),
  CONSTRAINT ck_votacao_status CHECK (status IN ('RASCUNHO','AGENDADA','ABERTA','ENCERRADA','CANCELADA'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Eleicao da unidade; votante elegivel e titular da unidade de origem';

-- Alternativas da votacao; publicar congela as opcoes
CREATE TABLE IF NOT EXISTS opcoes_votacao (
  id INT NOT NULL AUTO_INCREMENT,
  votacao_id INT NOT NULL,
  texto VARCHAR(200) NOT NULL,
  ordem INT NOT NULL,
  em_branco BOOLEAN NOT NULL DEFAULT FALSE,
  branco_unico INT GENERATED ALWAYS AS (CASE WHEN em_branco=1 THEN votacao_id ELSE NULL END) STORED,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (id),
  CONSTRAINT fk_opcao_votacao FOREIGN KEY (votacao_id) REFERENCES votacoes (id),
  UNIQUE KEY uq_opcao_ordem (votacao_id, ordem),
  UNIQUE KEY uq_opcao_composta (votacao_id, id),
  UNIQUE KEY uq_opcao_branco (branco_unico),
  CONSTRAINT ck_opcao_ordem CHECK (ordem > 0),
  CONSTRAINT ck_opcao_branco CHECK (em_branco IN (0,1))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Alternativas da votacao; publicar congela as opcoes';

-- Fotografia do eleitorado gerada uma vez na abertura
CREATE TABLE IF NOT EXISTS eleitores_votacao (
  id INT NOT NULL AUTO_INCREMENT,
  votacao_id INT NOT NULL,
  pessoa_id INT NOT NULL,
  membro_contrato_id INT NOT NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (id),
  CONSTRAINT fk_eleitor_votacao FOREIGN KEY (votacao_id) REFERENCES votacoes (id),
  CONSTRAINT fk_eleitor_membro_pessoa FOREIGN KEY (membro_contrato_id, pessoa_id) REFERENCES membros_contrato (id, pessoa_id),
  UNIQUE KEY uq_eleitor_pessoa (votacao_id, pessoa_id),
  UNIQUE KEY uq_eleitor_composto (votacao_id, id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Fotografia do eleitorado gerada uma vez na abertura';

-- Um voto por eleitor; nao expor vinculacao pessoal na API
CREATE TABLE IF NOT EXISTS votos (
  id INT NOT NULL AUTO_INCREMENT,
  votacao_id INT NOT NULL,
  eleitor_id INT NOT NULL,
  opcao_id INT NOT NULL,
  recibo CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (id),
  CONSTRAINT fk_voto_eleitor FOREIGN KEY (votacao_id, eleitor_id) REFERENCES eleitores_votacao (votacao_id, id),
  CONSTRAINT fk_voto_opcao FOREIGN KEY (votacao_id, opcao_id) REFERENCES opcoes_votacao (votacao_id, id),
  UNIQUE KEY uq_voto_eleitor (eleitor_id),
  UNIQUE KEY uq_voto_recibo (recibo),
  KEY ix_voto_apuracao (votacao_id, opcao_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Um voto por eleitor; nao expor vinculacao pessoal na API';

-- 7. NOTIFICACOES, AUDITORIA E IDEMPOTENCIA

-- Caixa de notificacoes persistentes por usuario
CREATE TABLE IF NOT EXISTS notificacoes (
  id INT NOT NULL AUTO_INCREMENT,
  usuario_id INT NOT NULL,
  clube_id INT NULL,
  categoria VARCHAR(40) NOT NULL,
  titulo VARCHAR(200) NOT NULL,
  mensagem TEXT NOT NULL,
  rota_interna VARCHAR(300) NULL,
  chave_evento VARCHAR(150) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  lida_em DATETIME(6) NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_notificacoes_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  CONSTRAINT fk_notif_usuario FOREIGN KEY (usuario_id) REFERENCES usuarios (id),
  CONSTRAINT fk_notif_clube FOREIGN KEY (clube_id) REFERENCES clubes (id),
  UNIQUE KEY uq_notif_evento (usuario_id, chave_evento),
  KEY ix_notif_naolida (usuario_id, lida_em, id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Caixa de notificacoes persistentes por usuario';

-- Append-only; API registra negocio sem senhas, tokens ou escolhas de voto
CREATE TABLE IF NOT EXISTS auditoria (
  id INT NOT NULL AUTO_INCREMENT,
  ator_usuario_id INT NULL,
  clube_id INT NULL,
  origem VARCHAR(10) NOT NULL,
  acao VARCHAR(100) NOT NULL,
  entidade VARCHAR(80) NOT NULL,
  referencia VARCHAR(100) NOT NULL,
  correlacao_id CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  alteracoes JSON NULL,
  observacao VARCHAR(1000) NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (id),
  CONSTRAINT fk_audit_ator FOREIGN KEY (ator_usuario_id) REFERENCES usuarios (id),
  CONSTRAINT fk_audit_clube FOREIGN KEY (clube_id) REFERENCES clubes (id),
  KEY ix_audit_entidade (entidade, referencia, created_at),
  KEY ix_audit_unidade (clube_id, created_at),
  KEY ix_audit_correlacao (correlacao_id),
  CONSTRAINT ck_audit_origem CHECK (origem IN ('USUARIO','SISTEMA','PROVEDOR'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Append-only; API registra negocio sem senhas, tokens ou escolhas de voto';

-- Resultado atomico por ator, rota, metodo e chave; nenhum segredo
CREATE TABLE IF NOT EXISTS requisicoes_idempotentes (
  id INT NOT NULL AUTO_INCREMENT,
  usuario_id INT NULL,
  escopo_autenticacao VARCHAR(80) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  rota VARCHAR(200) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  metodo VARCHAR(10) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  chave VARCHAR(100) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  payload_hash BINARY(32) NOT NULL,
  status VARCHAR(15) NOT NULL DEFAULT 'PROCESSANDO',
  http_status INT NULL,
  resposta JSON NULL,
  expira_em DATETIME(6) NOT NULL,
  created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  updated_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6),
  versao INT NOT NULL DEFAULT 1,
  CONSTRAINT ck_requisicoes_idempotentes_versao CHECK (versao >= 1),
  PRIMARY KEY (id),
  CONSTRAINT fk_idem_usuario FOREIGN KEY (usuario_id) REFERENCES usuarios (id),
  UNIQUE KEY uq_idem_chave (escopo_autenticacao, rota, metodo, chave),
  KEY ix_idem_expira (expira_em),
  CONSTRAINT ck_idem_metodo CHECK (metodo IN ('POST','PUT','PATCH','DELETE')),
  CONSTRAINT ck_idem_status CHECK (status IN ('PROCESSANDO','CONCLUIDA','FALHOU')),
  CONSTRAINT ck_idem_http CHECK (http_status IS NULL OR http_status BETWEEN 100 AND 599)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='Resultado atomico por ator, rota, metodo e chave; nenhum segredo';

-- 8. GATILHOS DE INTEGRIDADE E HISTORICO
-- Nao substituem autorizacao da API. Sem SQL dinamico ou senhas de exemplo.
DELIMITER $$

CREATE TRIGGER IF NOT EXISTS bu_estados
BEFORE UPDATE ON estados FOR EACH ROW
BEGIN
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_cidades
BEFORE UPDATE ON cidades FOR EACH ROW
BEGIN
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_clubes
BEFORE UPDATE ON clubes FOR EACH ROW
BEGIN
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_pessoas
BEFORE UPDATE ON pessoas FOR EACH ROW
BEGIN
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_usuarios
BEFORE UPDATE ON usuarios FOR EACH ROW
BEGIN
  SET NEW.email_login = TRIM(NEW.email_login);
  SET NEW.email_normalizado = LOWER(NEW.email_login);
  IF NEW.perfil <> 'ADM_LOCAL' AND EXISTS (SELECT 1 FROM administradores_clubes WHERE usuario_id=OLD.id AND ativo=1) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Remova o vinculo administrativo local antes de alterar o perfil.';
  END IF;
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_administradores_clubes
BEFORE UPDATE ON administradores_clubes FOR EACH ROW
BEGIN
  IF NOT (NEW.usuario_id <=> OLD.usuario_id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_sessoes
BEFORE UPDATE ON sessoes FOR EACH ROW
BEGIN
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_tokens_usuario
BEFORE UPDATE ON tokens_usuario FOR EACH ROW
BEGIN
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_interesses
BEFORE UPDATE ON interesses FOR EACH ROW
BEGIN
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_arquivos
BEFORE UPDATE ON arquivos FOR EACH ROW
BEGIN
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_documentos
BEFORE UPDATE ON documentos FOR EACH ROW
BEGIN
  IF OLD.publicado_em IS NOT NULL THEN
    IF NOT (NEW.tipo <=> OLD.tipo) OR
       NOT (NEW.codigo_versao <=> OLD.codigo_versao) OR
       NOT (NEW.titulo <=> OLD.titulo) OR
       NOT (NEW.conteudo <=> OLD.conteudo) OR
       NOT (NEW.sha256 <=> OLD.sha256) OR
       NOT (NEW.publicado_em <=> OLD.publicado_em) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Texto publicado e imutavel. Cadastre nova versao do documento.';
    END IF;
  END IF;
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_planos
BEFORE UPDATE ON planos FOR EACH ROW
BEGIN
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_plano_versoes
BEFORE UPDATE ON plano_versoes FOR EACH ROW
BEGIN
  IF OLD.publicada_em IS NOT NULL THEN
    IF NOT (NEW.plano_id <=> OLD.plano_id) OR
       NOT (NEW.numero_versao <=> OLD.numero_versao) OR
       NOT (NEW.nome <=> OLD.nome) OR
       NOT (NEW.descricao <=> OLD.descricao) OR
       NOT (NEW.abrangencia <=> OLD.abrangencia) OR
       NOT (NEW.duracao_meses <=> OLD.duracao_meses) OR
       NOT (NEW.limite_beneficiarios <=> OLD.limite_beneficiarios) OR
       NOT (NEW.limite_dependentes <=> OLD.limite_dependentes) OR
       NOT (NEW.limite_convidados <=> OLD.limite_convidados) OR
       NOT (NEW.carencia_dias <=> OLD.carencia_dias) OR
       NOT (NEW.limite_reservas_futuras <=> OLD.limite_reservas_futuras) OR
       NOT (NEW.beneficios <=> OLD.beneficios) OR
       NOT (NEW.regras <=> OLD.regras) OR
       NOT (NEW.documento_termos_id <=> OLD.documento_termos_id) OR
       NOT (NEW.publicada_em <=> OLD.publicada_em) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Plano publicado e imutavel. Crie uma nova versao.';
    END IF;
    IF NEW.status NOT IN ('PUBLICADA','DESATIVADA') THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Versao publicada nao pode voltar a rascunho.';
    END IF;
  END IF;
  IF OLD.status='RASCUNHO' AND NEW.status='PUBLICADA' THEN
    IF NOT EXISTS (SELECT 1 FROM condicoes_pagamento WHERE plano_versao_id=OLD.id) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Cadastre ao menos uma condicao de pagamento antes de publicar.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM documentos WHERE id=NEW.documento_termos_id AND publicado_em IS NOT NULL) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Publique o documento de termos antes de publicar o plano.';
    END IF;
  END IF;
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_condicoes_pagamento
BEFORE UPDATE ON condicoes_pagamento FOR EACH ROW
BEGIN
  IF NEW.plano_versao_id <> OLD.plano_versao_id THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Uma condicao nao pode ser transferida para outra versao.';
  END IF;
  IF EXISTS (SELECT 1 FROM plano_versoes WHERE id=OLD.plano_versao_id AND publicada_em IS NOT NULL) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Condicao publicada e imutavel. Crie uma nova versao do plano.';
  END IF;
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_contratos
BEFORE UPDATE ON contratos FOR EACH ROW
BEGIN
  IF NOT (NEW.numero <=> OLD.numero) OR
       NOT (NEW.titular_pessoa_id <=> OLD.titular_pessoa_id) OR
       NOT (NEW.clube_origem_id <=> OLD.clube_origem_id) OR
       NOT (NEW.plano_versao_id <=> OLD.plano_versao_id) OR
       NOT (NEW.condicao_pagamento_id <=> OLD.condicao_pagamento_id) OR
       NOT (NEW.contrato_anterior_id <=> OLD.contrato_anterior_id) OR
       NOT (NEW.data_inicio <=> OLD.data_inicio) OR
       NOT (NEW.data_fim <=> OLD.data_fim) OR
       NOT (NEW.valor_total_centavos <=> OLD.valor_total_centavos) OR
       NOT (NEW.quantidade_parcelas <=> OLD.quantidade_parcelas) OR
       NOT (NEW.dia_vencimento <=> OLD.dia_vencimento) OR
       NOT (NEW.moeda <=> OLD.moeda) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Condicoes contratuais sao imutaveis. Renove com novo contrato.';
    END IF;
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_membros_contrato
BEFORE UPDATE ON membros_contrato FOR EACH ROW
BEGIN
  IF NOT (NEW.contrato_id <=> OLD.contrato_id) OR
       NOT (NEW.pessoa_id <=> OLD.pessoa_id) OR
       NOT (NEW.tipo <=> OLD.tipo) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Vinculo e categoria do membro sao imutaveis; preserve o historico.';
    END IF;
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_solicitacoes_vinculo
BEFORE UPDATE ON solicitacoes_vinculo FOR EACH ROW
BEGIN
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_faturas
BEFORE UPDATE ON faturas FOR EACH ROW
BEGIN
  IF NOT (NEW.contrato_id <=> OLD.contrato_id) OR
       NOT (NEW.numero <=> OLD.numero) OR
       NOT (NEW.numero_parcela <=> OLD.numero_parcela) OR
       NOT (NEW.descricao <=> OLD.descricao) OR
       NOT (NEW.vencimento <=> OLD.vencimento) OR
       NOT (NEW.valor_centavos <=> OLD.valor_centavos) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Dados da parcela sao imutaveis. Nao reescreva o historico financeiro.';
    END IF;
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_pagamentos
BEFORE UPDATE ON pagamentos FOR EACH ROW
BEGIN
  IF OLD.status='CONFIRMADO' THEN
    IF NOT (NEW.fatura_id <=> OLD.fatura_id) OR
       NOT (NEW.origem <=> OLD.origem) OR
       NOT (NEW.meio <=> OLD.meio) OR
       NOT (NEW.valor_centavos <=> OLD.valor_centavos) OR
       NOT (NEW.moeda <=> OLD.moeda) OR
       NOT (NEW.status <=> OLD.status) OR
       NOT (NEW.provedor <=> OLD.provedor) OR
       NOT (NEW.referencia_externa <=> OLD.referencia_externa) OR
       NOT (NEW.referencia_manual <=> OLD.referencia_manual) OR
       NOT (NEW.comprovante_arquivo_id <=> OLD.comprovante_arquivo_id) OR
       NOT (NEW.registrado_por_usuario_id <=> OLD.registrado_por_usuario_id) OR
       NOT (NEW.confirmado_por_usuario_id <=> OLD.confirmado_por_usuario_id) OR
       NOT (NEW.pago_em <=> OLD.pago_em) OR
       NOT (NEW.confirmado_em <=> OLD.confirmado_em) OR
       NOT (NEW.observacao <=> OLD.observacao) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Pagamento confirmado nao pode ser reescrito.';
    END IF;
    IF OLD.quitacao_aplicada=1 AND NEW.quitacao_aplicada=0 AND NOT EXISTS
      (SELECT 1 FROM estornos WHERE pagamento_id=OLD.id AND status='CONFIRMADO') THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Liberar quitacao exige estorno confirmado em transacao.';
    END IF;
  END IF;
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_estornos
BEFORE UPDATE ON estornos FOR EACH ROW
BEGIN
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_eventos_provedor
BEFORE UPDATE ON eventos_provedor FOR EACH ROW
BEGIN
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_visitantes
BEFORE UPDATE ON visitantes FOR EACH ROW
BEGIN
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_tipos_area
BEFORE UPDATE ON tipos_area FOR EACH ROW
BEGIN
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_areas
BEFORE UPDATE ON areas FOR EACH ROW
BEGIN
  DECLARE teto INT;
  SELECT max_minutos INTO teto FROM tipos_area WHERE id=NEW.tipo_area_id;
  IF teto IS NOT NULL AND (NEW.max_minutos IS NULL OR NEW.max_minutos > teto) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Area esportiva ou de jogos deve limitar a reserva a no maximo 180 minutos.';
  END IF;
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_horarios_area
BEFORE UPDATE ON horarios_area FOR EACH ROW
BEGIN
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_eventos
BEFORE UPDATE ON eventos FOR EACH ROW
BEGIN
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_inscricoes_evento
BEFORE UPDATE ON inscricoes_evento FOR EACH ROW
BEGIN
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_reservas
BEFORE UPDATE ON reservas FOR EACH ROW
BEGIN
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_ocupacoes_area
BEFORE UPDATE ON ocupacoes_area FOR EACH ROW
BEGIN
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_chamados
BEFORE UPDATE ON chamados FOR EACH ROW
BEGIN
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_mensagens_chat
BEFORE UPDATE ON mensagens_chat FOR EACH ROW
BEGIN
  IF NOT (NEW.clube_id <=> OLD.clube_id) OR
       NOT (NEW.autor_usuario_id <=> OLD.autor_usuario_id) OR
       NOT (NEW.autor_nome <=> OLD.autor_nome) OR
       NOT (NEW.autor_perfil <=> OLD.autor_perfil) OR
       NOT (NEW.texto <=> OLD.texto) OR
       NOT (NEW.chave_cliente <=> OLD.chave_cliente) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Mensagem original e autoria sao imutaveis. Use moderacao.';
    END IF;
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_leituras_chat
BEFORE UPDATE ON leituras_chat FOR EACH ROW
BEGIN
  IF NOT (NEW.clube_id <=> OLD.clube_id) OR
       NOT (NEW.usuario_id <=> OLD.usuario_id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_moderacoes_chat
BEFORE UPDATE ON moderacoes_chat FOR EACH ROW
BEGIN
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_denuncias_chat
BEFORE UPDATE ON denuncias_chat FOR EACH ROW
BEGIN
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_votacoes
BEFORE UPDATE ON votacoes FOR EACH ROW
BEGIN
  IF OLD.status <> 'RASCUNHO' THEN
    IF NOT (NEW.clube_id <=> OLD.clube_id) OR
       NOT (NEW.criada_por_usuario_id <=> OLD.criada_por_usuario_id) OR
       NOT (NEW.motivo <=> OLD.motivo) OR
       NOT (NEW.descricao <=> OLD.descricao) OR
       NOT (NEW.regras <=> OLD.regras) OR
       NOT (NEW.inicio_em <=> OLD.inicio_em) OR
       NOT (NEW.fim_em <=> OLD.fim_em) OR
       NOT (NEW.exige_adimplencia <=> OLD.exige_adimplencia) OR
       NOT (NEW.quorum_percentual <=> OLD.quorum_percentual) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Votacao publicada tem regras e periodo imutaveis.';
    END IF;
    IF NEW.status='RASCUNHO' THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Votacao publicada nao volta a rascunho.';
    END IF;
  END IF;
  IF OLD.status IN ('ENCERRADA','CANCELADA') AND NEW.status <> OLD.status THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Votacao encerrada ou cancelada nao pode ser reaberta.';
  END IF;
  IF OLD.status='RASCUNHO' AND NEW.status <> 'RASCUNHO' AND NEW.status <> 'CANCELADA' THEN
    IF (SELECT COUNT(*) FROM opcoes_votacao WHERE votacao_id=OLD.id) < 2 THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Cadastre ao menos duas alternativas antes de publicar a votacao.';
    END IF;
  END IF;
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_notificacoes
BEFORE UPDATE ON notificacoes FOR EACH ROW
BEGIN
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bu_requisicoes_idempotentes
BEFORE UPDATE ON requisicoes_idempotentes FOR EACH ROW
BEGIN
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Identificador e data de criacao sao imutaveis.';
    END IF;
  SET NEW.versao = OLD.versao + 1;
  SET NEW.updated_at = UTC_TIMESTAMP(6);
END$$

CREATE TRIGGER IF NOT EXISTS bi_usuarios
BEFORE INSERT ON usuarios FOR EACH ROW
BEGIN
  SET NEW.email_login = TRIM(NEW.email_login);
  SET NEW.email_normalizado = LOWER(NEW.email_login);
END$$

CREATE TRIGGER IF NOT EXISTS bi_admin_perfil
BEFORE INSERT ON administradores_clubes FOR EACH ROW
BEGIN
  IF NOT EXISTS (SELECT 1 FROM usuarios WHERE id=NEW.usuario_id AND perfil='ADM_LOCAL') THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Vinculo de unidade exige usuario ADM_LOCAL.';
  END IF;
END$$

CREATE TRIGGER IF NOT EXISTS bu_admin_perfil
BEFORE UPDATE ON administradores_clubes FOR EACH ROW
BEGIN
  IF NOT EXISTS (SELECT 1 FROM usuarios WHERE id=NEW.usuario_id AND perfil='ADM_LOCAL') THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Vinculo de unidade exige usuario ADM_LOCAL.';
  END IF;
END$$

CREATE TRIGGER IF NOT EXISTS bi_plano_versoes
BEFORE INSERT ON plano_versoes FOR EACH ROW
BEGIN
  IF NEW.status <> 'RASCUNHO' OR NEW.publicada_em IS NOT NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Crie a versao como RASCUNHO, cadastre condicoes e depois publique.';
  END IF;
END$$

CREATE TRIGGER IF NOT EXISTS bi_condicoes_pagamento
BEFORE INSERT ON condicoes_pagamento FOR EACH ROW
BEGIN
  IF EXISTS (SELECT 1 FROM plano_versoes WHERE id=NEW.plano_versao_id AND publicada_em IS NOT NULL) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Nao se acrescenta condicao a versao ja publicada.';
  END IF;
END$$

CREATE TRIGGER IF NOT EXISTS bd_condicoes_pagamento
BEFORE DELETE ON condicoes_pagamento FOR EACH ROW
BEGIN
  IF EXISTS (SELECT 1 FROM plano_versoes WHERE id=OLD.plano_versao_id AND publicada_em IS NOT NULL) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Nao se exclui condicao de uma versao publicada.';
  END IF;
END$$

CREATE TRIGGER IF NOT EXISTS bi_contratos
BEFORE INSERT ON contratos FOR EACH ROW
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM condicoes_pagamento cp
    JOIN plano_versoes pv ON pv.id=cp.plano_versao_id
    JOIN planos p ON p.id=pv.plano_id
    WHERE cp.id=NEW.condicao_pagamento_id AND cp.plano_versao_id=NEW.plano_versao_id
      AND pv.status='PUBLICADA' AND p.ativo=1
      AND cp.valor_total_centavos=NEW.valor_total_centavos
      AND cp.quantidade_parcelas=NEW.quantidade_parcelas
  ) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Contrato exige oferta publicada e valores iguais a condicao selecionada.';
  END IF;
END$$

CREATE TRIGGER IF NOT EXISTS bi_membros_contrato
BEFORE INSERT ON membros_contrato FOR EACH ROW
BEGIN
  DECLARE titular INT;
  SELECT titular_pessoa_id INTO titular FROM contratos WHERE id=NEW.contrato_id;
  IF (NEW.tipo='TITULAR' AND NEW.pessoa_id <> titular)
     OR (NEW.tipo <> 'TITULAR' AND NEW.pessoa_id=titular) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='A categoria TITULAR deve corresponder ao titular do contrato.';
  END IF;
END$$

CREATE TRIGGER IF NOT EXISTS bi_areas
BEFORE INSERT ON areas FOR EACH ROW
BEGIN
  DECLARE teto INT;
  SELECT max_minutos INTO teto FROM tipos_area WHERE id=NEW.tipo_area_id;
  IF teto IS NOT NULL AND (NEW.max_minutos IS NULL OR NEW.max_minutos > teto) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Area esportiva ou de jogos deve limitar a reserva a no maximo 180 minutos.';
  END IF;
END$$

CREATE TRIGGER IF NOT EXISTS bi_opcoes_votacao
BEFORE INSERT ON opcoes_votacao FOR EACH ROW
BEGIN
  IF EXISTS (SELECT 1 FROM votacoes WHERE id=NEW.votacao_id AND status <> 'RASCUNHO') THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Alternativas de votacao publicada sao imutaveis.';
  END IF;
END$$

CREATE TRIGGER IF NOT EXISTS bu_opcoes_votacao
BEFORE UPDATE ON opcoes_votacao FOR EACH ROW
BEGIN
  IF EXISTS (SELECT 1 FROM votacoes WHERE id=OLD.votacao_id AND status <> 'RASCUNHO') THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Alternativas de votacao publicada sao imutaveis.';
  END IF;
  IF NOT (NEW.id <=> OLD.id) OR
       NOT (NEW.votacao_id <=> OLD.votacao_id) OR
       NOT (NEW.created_at <=> OLD.created_at) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Nao transfira alternativas entre votacoes.';
    END IF;
END$$

CREATE TRIGGER IF NOT EXISTS bd_opcoes_votacao
BEFORE DELETE ON opcoes_votacao FOR EACH ROW
BEGIN
  IF EXISTS (SELECT 1 FROM votacoes WHERE id=OLD.votacao_id AND status <> 'RASCUNHO') THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Alternativas de votacao publicada sao imutaveis.';
  END IF;
END$$

CREATE TRIGGER IF NOT EXISTS bi_eleitores_votacao
BEFORE INSERT ON eleitores_votacao FOR EACH ROW
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM membros_contrato m
    JOIN contratos c ON c.id=m.contrato_id
    JOIN votacoes v ON v.id=NEW.votacao_id
    WHERE m.id=NEW.membro_contrato_id AND m.pessoa_id=NEW.pessoa_id
      AND m.tipo='TITULAR' AND m.status='ATIVO' AND c.status='VIGENTE'
      AND c.clube_origem_id=v.clube_id
      AND v.status IN ('AGENDADA','ABERTA') AND v.eleitorado_gerado_em IS NULL
  ) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Eleitor deve ser titular ativo da unidade; fotografia ainda deve estar aberta.';
  END IF;
END$$

CREATE TRIGGER IF NOT EXISTS bi_votos
BEFORE INSERT ON votos FOR EACH ROW
BEGIN
  IF NOT EXISTS (SELECT 1 FROM votacoes
    WHERE id=NEW.votacao_id AND status='ABERTA' AND eleitorado_gerado_em IS NOT NULL
      AND UTC_TIMESTAMP(6) >= inicio_em AND UTC_TIMESTAMP(6) < fim_em) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Votacao nao esta aberta no periodo ou o eleitorado nao foi finalizado.';
  END IF;
END$$

CREATE TRIGGER IF NOT EXISTS bu_votos
BEFORE UPDATE ON votos FOR EACH ROW
BEGIN
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Registro historico: alteracao ou exclusao direta nao permitida.';
END$$

CREATE TRIGGER IF NOT EXISTS bd_votos
BEFORE DELETE ON votos FOR EACH ROW
BEGIN
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Registro historico: alteracao ou exclusao direta nao permitida.';
END$$

CREATE TRIGGER IF NOT EXISTS bu_eleitores_votacao
BEFORE UPDATE ON eleitores_votacao FOR EACH ROW
BEGIN
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Registro historico: alteracao ou exclusao direta nao permitida.';
END$$

CREATE TRIGGER IF NOT EXISTS bd_eleitores_votacao
BEFORE DELETE ON eleitores_votacao FOR EACH ROW
BEGIN
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Registro historico: alteracao ou exclusao direta nao permitida.';
END$$

CREATE TRIGGER IF NOT EXISTS bu_mensagens_chamado
BEFORE UPDATE ON mensagens_chamado FOR EACH ROW
BEGIN
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Registro historico: alteracao ou exclusao direta nao permitida.';
END$$

CREATE TRIGGER IF NOT EXISTS bd_mensagens_chamado
BEFORE DELETE ON mensagens_chamado FOR EACH ROW
BEGIN
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Registro historico: alteracao ou exclusao direta nao permitida.';
END$$

CREATE TRIGGER IF NOT EXISTS bu_aceites_documentos
BEFORE UPDATE ON aceites_documentos FOR EACH ROW
BEGIN
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Registro historico: alteracao ou exclusao direta nao permitida.';
END$$

CREATE TRIGGER IF NOT EXISTS bd_aceites_documentos
BEFORE DELETE ON aceites_documentos FOR EACH ROW
BEGIN
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Registro historico: alteracao ou exclusao direta nao permitida.';
END$$

CREATE TRIGGER IF NOT EXISTS bu_auditoria
BEFORE UPDATE ON auditoria FOR EACH ROW
BEGIN
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Registro historico: alteracao ou exclusao direta nao permitida.';
END$$

CREATE TRIGGER IF NOT EXISTS bd_auditoria
BEFORE DELETE ON auditoria FOR EACH ROW
BEGIN
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Registro historico: alteracao ou exclusao direta nao permitida.';
END$$

CREATE TRIGGER IF NOT EXISTS bd_contratos
BEFORE DELETE ON contratos FOR EACH ROW
BEGIN
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Preserve o historico: use o status de cancelamento, remocao ou inativacao.';
END$$

CREATE TRIGGER IF NOT EXISTS bd_membros_contrato
BEFORE DELETE ON membros_contrato FOR EACH ROW
BEGIN
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Preserve o historico: use o status de cancelamento, remocao ou inativacao.';
END$$

CREATE TRIGGER IF NOT EXISTS bd_solicitacoes_vinculo
BEFORE DELETE ON solicitacoes_vinculo FOR EACH ROW
BEGIN
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Preserve o historico: use o status de cancelamento, remocao ou inativacao.';
END$$

CREATE TRIGGER IF NOT EXISTS bd_faturas
BEFORE DELETE ON faturas FOR EACH ROW
BEGIN
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Preserve o historico: use o status de cancelamento, remocao ou inativacao.';
END$$

CREATE TRIGGER IF NOT EXISTS bd_pagamentos
BEFORE DELETE ON pagamentos FOR EACH ROW
BEGIN
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Preserve o historico: use o status de cancelamento, remocao ou inativacao.';
END$$

CREATE TRIGGER IF NOT EXISTS bd_estornos
BEFORE DELETE ON estornos FOR EACH ROW
BEGIN
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Preserve o historico: use o status de cancelamento, remocao ou inativacao.';
END$$

CREATE TRIGGER IF NOT EXISTS bd_reservas
BEFORE DELETE ON reservas FOR EACH ROW
BEGIN
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Preserve o historico: use o status de cancelamento, remocao ou inativacao.';
END$$

CREATE TRIGGER IF NOT EXISTS bd_ocupacoes_area
BEFORE DELETE ON ocupacoes_area FOR EACH ROW
BEGIN
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Preserve o historico: use o status de cancelamento, remocao ou inativacao.';
END$$

CREATE TRIGGER IF NOT EXISTS bd_chamados
BEFORE DELETE ON chamados FOR EACH ROW
BEGIN
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Preserve o historico: use o status de cancelamento, remocao ou inativacao.';
END$$

CREATE TRIGGER IF NOT EXISTS bd_eventos
BEFORE DELETE ON eventos FOR EACH ROW
BEGIN
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Preserve o historico: use o status de cancelamento, remocao ou inativacao.';
END$$

CREATE TRIGGER IF NOT EXISTS bd_votacoes
BEFORE DELETE ON votacoes FOR EACH ROW
BEGIN
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Preserve o historico: use o status de cancelamento, remocao ou inativacao.';
END$$

CREATE TRIGGER IF NOT EXISTS bd_plano_versoes
BEFORE DELETE ON plano_versoes FOR EACH ROW
BEGIN
  IF OLD.publicada_em IS NOT NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Registro publicado nao pode ser excluido; desative-o.';
  END IF;
END$$

CREATE TRIGGER IF NOT EXISTS bd_documentos
BEFORE DELETE ON documentos FOR EACH ROW
BEGIN
  IF OLD.publicado_em IS NOT NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT='Registro publicado nao pode ser excluido; desative-o.';
  END IF;
END$$

DELIMITER ;

-- 9. VISTAS DE CONSULTA (SQL SECURITY INVOKER)
-- Vistas NAO filtram o usuario logado nem concedem autorizacao por unidade.
-- A API deve sempre aplicar o escopo da sessao antes de serializar os dados.

CREATE OR REPLACE SQL SECURITY INVOKER VIEW vw_contratos_detalhados AS
SELECT c.*, pv.plano_id, pv.numero_versao AS numero_versao_plano,
       pv.nome AS plano_nome, pv.descricao AS plano_descricao, pv.abrangencia,
       pv.duracao_meses, pv.limite_beneficiarios, pv.limite_dependentes,
       pv.limite_convidados, pv.carencia_dias, pv.limite_reservas_futuras,
       pv.beneficios, pv.regras, pv.documento_termos_id,
       cp.nome AS condicao_nome, cp.meio AS meio_pagamento, cp.intervalo_meses
FROM contratos c
JOIN plano_versoes pv ON pv.id=c.plano_versao_id
JOIN condicoes_pagamento cp ON cp.id=c.condicao_pagamento_id;

CREATE OR REPLACE SQL SECURITY INVOKER VIEW vw_planos_publicos AS
SELECT p.id AS plano_id, p.codigo, p.versao AS versao_catalogo,
       pv.id AS plano_versao_id, pv.numero_versao, pv.nome, pv.descricao,
       pv.abrangencia, pv.duracao_meses, pv.limite_beneficiarios,
       pv.limite_dependentes, pv.limite_convidados, pv.carencia_dias,
       pv.limite_reservas_futuras, pv.beneficios, pv.regras, pv.documento_termos_id
FROM planos p JOIN plano_versoes pv ON pv.plano_id=p.id
WHERE p.ativo=1 AND pv.status='PUBLICADA'
  AND pv.numero_versao=(SELECT MAX(p2.numero_versao) FROM plano_versoes p2
                       WHERE p2.plano_id=p.id AND p2.status='PUBLICADA');

CREATE OR REPLACE SQL SECURITY INVOKER VIEW vw_reservas_detalhadas AS
SELECT r.*, o.area_id, o.clube_id, o.inicio_em, o.fim_em,
       o.intervalo_apos_minutos, o.status AS status_ocupacao, a.nome AS area_nome
FROM reservas r JOIN ocupacoes_area o ON o.reserva_id=r.id
JOIN areas a ON a.id=o.area_id;

CREATE OR REPLACE SQL SECURITY INVOKER VIEW vw_votacoes_resultados AS
SELECT o.votacao_id, o.id AS opcao_id, o.texto, o.ordem, o.em_branco,
       COUNT(v.id) AS quantidade_votos
FROM opcoes_votacao o LEFT JOIN votos v ON v.votacao_id=o.votacao_id AND v.opcao_id=o.id
GROUP BY o.votacao_id, o.id, o.texto, o.ordem, o.em_branco;

-- Atraso nao e persistido. A API compara fatura.vencimento com a data atual
-- NO FUSO DA UNIDADE, projetando Paga/Pendente/Atrasada. Evita job diario
-- e nao depende de carregar tabelas de fuso horario no MySQL do Windows.

-- 10. CATALOGOS REAIS (SEM DADOS DE DEMONSTRACAO)
-- Insercoes condicionais preservam valores que ja existam na reexecucao.

INSERT INTO estados (nome, uf) SELECT 'Acre', 'AC' WHERE NOT EXISTS (SELECT 1 FROM estados WHERE uf='AC');

INSERT INTO estados (nome, uf) SELECT 'Alagoas', 'AL' WHERE NOT EXISTS (SELECT 1 FROM estados WHERE uf='AL');

INSERT INTO estados (nome, uf) SELECT 'Amapá', 'AP' WHERE NOT EXISTS (SELECT 1 FROM estados WHERE uf='AP');

INSERT INTO estados (nome, uf) SELECT 'Amazonas', 'AM' WHERE NOT EXISTS (SELECT 1 FROM estados WHERE uf='AM');

INSERT INTO estados (nome, uf) SELECT 'Bahia', 'BA' WHERE NOT EXISTS (SELECT 1 FROM estados WHERE uf='BA');

INSERT INTO estados (nome, uf) SELECT 'Ceará', 'CE' WHERE NOT EXISTS (SELECT 1 FROM estados WHERE uf='CE');

INSERT INTO estados (nome, uf) SELECT 'Distrito Federal', 'DF' WHERE NOT EXISTS (SELECT 1 FROM estados WHERE uf='DF');

INSERT INTO estados (nome, uf) SELECT 'Espírito Santo', 'ES' WHERE NOT EXISTS (SELECT 1 FROM estados WHERE uf='ES');

INSERT INTO estados (nome, uf) SELECT 'Goiás', 'GO' WHERE NOT EXISTS (SELECT 1 FROM estados WHERE uf='GO');

INSERT INTO estados (nome, uf) SELECT 'Maranhão', 'MA' WHERE NOT EXISTS (SELECT 1 FROM estados WHERE uf='MA');

INSERT INTO estados (nome, uf) SELECT 'Mato Grosso', 'MT' WHERE NOT EXISTS (SELECT 1 FROM estados WHERE uf='MT');

INSERT INTO estados (nome, uf) SELECT 'Mato Grosso do Sul', 'MS' WHERE NOT EXISTS (SELECT 1 FROM estados WHERE uf='MS');

INSERT INTO estados (nome, uf) SELECT 'Minas Gerais', 'MG' WHERE NOT EXISTS (SELECT 1 FROM estados WHERE uf='MG');

INSERT INTO estados (nome, uf) SELECT 'Pará', 'PA' WHERE NOT EXISTS (SELECT 1 FROM estados WHERE uf='PA');

INSERT INTO estados (nome, uf) SELECT 'Paraíba', 'PB' WHERE NOT EXISTS (SELECT 1 FROM estados WHERE uf='PB');

INSERT INTO estados (nome, uf) SELECT 'Paraná', 'PR' WHERE NOT EXISTS (SELECT 1 FROM estados WHERE uf='PR');

INSERT INTO estados (nome, uf) SELECT 'Pernambuco', 'PE' WHERE NOT EXISTS (SELECT 1 FROM estados WHERE uf='PE');

INSERT INTO estados (nome, uf) SELECT 'Piauí', 'PI' WHERE NOT EXISTS (SELECT 1 FROM estados WHERE uf='PI');

INSERT INTO estados (nome, uf) SELECT 'Rio de Janeiro', 'RJ' WHERE NOT EXISTS (SELECT 1 FROM estados WHERE uf='RJ');

INSERT INTO estados (nome, uf) SELECT 'Rio Grande do Norte', 'RN' WHERE NOT EXISTS (SELECT 1 FROM estados WHERE uf='RN');

INSERT INTO estados (nome, uf) SELECT 'Rio Grande do Sul', 'RS' WHERE NOT EXISTS (SELECT 1 FROM estados WHERE uf='RS');

INSERT INTO estados (nome, uf) SELECT 'Rondônia', 'RO' WHERE NOT EXISTS (SELECT 1 FROM estados WHERE uf='RO');

INSERT INTO estados (nome, uf) SELECT 'Roraima', 'RR' WHERE NOT EXISTS (SELECT 1 FROM estados WHERE uf='RR');

INSERT INTO estados (nome, uf) SELECT 'Santa Catarina', 'SC' WHERE NOT EXISTS (SELECT 1 FROM estados WHERE uf='SC');

INSERT INTO estados (nome, uf) SELECT 'São Paulo', 'SP' WHERE NOT EXISTS (SELECT 1 FROM estados WHERE uf='SP');

INSERT INTO estados (nome, uf) SELECT 'Sergipe', 'SE' WHERE NOT EXISTS (SELECT 1 FROM estados WHERE uf='SE');

INSERT INTO estados (nome, uf) SELECT 'Tocantins', 'TO' WHERE NOT EXISTS (SELECT 1 FROM estados WHERE uf='TO');

INSERT INTO interesses (nome) SELECT 'Futebol' WHERE NOT EXISTS (SELECT 1 FROM interesses WHERE nome='Futebol');

INSERT INTO interesses (nome) SELECT 'Tênis' WHERE NOT EXISTS (SELECT 1 FROM interesses WHERE nome='Tênis');

INSERT INTO interesses (nome) SELECT 'Vôlei' WHERE NOT EXISTS (SELECT 1 FROM interesses WHERE nome='Vôlei');

INSERT INTO interesses (nome) SELECT 'Beach tennis' WHERE NOT EXISTS (SELECT 1 FROM interesses WHERE nome='Beach tennis');

INSERT INTO interesses (nome) SELECT 'Natação' WHERE NOT EXISTS (SELECT 1 FROM interesses WHERE nome='Natação');

INSERT INTO interesses (nome) SELECT 'Academia' WHERE NOT EXISTS (SELECT 1 FROM interesses WHERE nome='Academia');

INSERT INTO interesses (nome) SELECT 'Jogos de salão' WHERE NOT EXISTS (SELECT 1 FROM interesses WHERE nome='Jogos de salão');

INSERT INTO interesses (nome) SELECT 'Gastronomia' WHERE NOT EXISTS (SELECT 1 FROM interesses WHERE nome='Gastronomia');

INSERT INTO interesses (nome) SELECT 'Pesca' WHERE NOT EXISTS (SELECT 1 FROM interesses WHERE nome='Pesca');

INSERT INTO interesses (nome) SELECT 'Eventos culturais' WHERE NOT EXISTS (SELECT 1 FROM interesses WHERE nome='Eventos culturais');

INSERT INTO tipos_area (codigo, nome, max_minutos) SELECT 'FUTEBOL', 'Campo de Futebol', 180 WHERE NOT EXISTS (SELECT 1 FROM tipos_area WHERE codigo='FUTEBOL');

INSERT INTO tipos_area (codigo, nome, max_minutos) SELECT 'TENIS', 'Quadra de Tênis', 180 WHERE NOT EXISTS (SELECT 1 FROM tipos_area WHERE codigo='TENIS');

INSERT INTO tipos_area (codigo, nome, max_minutos) SELECT 'POLIESPORTIVA', 'Quadra Poliesportiva', 180 WHERE NOT EXISTS (SELECT 1 FROM tipos_area WHERE codigo='POLIESPORTIVA');

INSERT INTO tipos_area (codigo, nome, max_minutos) SELECT 'AREIA', 'Quadra de Areia', 180 WHERE NOT EXISTS (SELECT 1 FROM tipos_area WHERE codigo='AREIA');

INSERT INTO tipos_area (codigo, nome, max_minutos) SELECT 'JOGOS', 'Sala de Jogos', 180 WHERE NOT EXISTS (SELECT 1 FROM tipos_area WHERE codigo='JOGOS');

INSERT INTO tipos_area (codigo, nome, max_minutos) SELECT 'ACADEMIA', 'Academia', 180 WHERE NOT EXISTS (SELECT 1 FROM tipos_area WHERE codigo='ACADEMIA');

INSERT INTO tipos_area (codigo, nome, max_minutos) SELECT 'PISCINA', 'Piscina', 180 WHERE NOT EXISTS (SELECT 1 FROM tipos_area WHERE codigo='PISCINA');

INSERT INTO tipos_area (codigo, nome, max_minutos) SELECT 'QUIOSQUE', 'Quiosque e Churrasqueira', 180 WHERE NOT EXISTS (SELECT 1 FROM tipos_area WHERE codigo='QUIOSQUE');

INSERT INTO tipos_area (codigo, nome, max_minutos) SELECT 'SALAO_FESTA', 'Salão de Festa', NULL WHERE NOT EXISTS (SELECT 1 FROM tipos_area WHERE codigo='SALAO_FESTA');

INSERT INTO tipos_area (codigo, nome, max_minutos) SELECT 'SALAO_SOCIAL', 'Salão Social', NULL WHERE NOT EXISTS (SELECT 1 FROM tipos_area WHERE codigo='SALAO_SOCIAL');

-- 11. CONTA TECNICA LOCAL DA FUTURA API
-- Somente DML neste banco; sem privilegios globais/DDL/GRANT OPTION.
-- Se ja existir, CREATE USER IF NOT EXISTS NAO redefine a senha.
-- A API conectara via TCP a 127.0.0.1, usando a porta configurada no servidor.
-- Em uma implantacao futura com API em outra maquina, configurar outra origem
-- restrita e TLS. Nao trocar este host por '%' por conveniencia.
CREATE USER IF NOT EXISTS 'vanderclub_api'@'127.0.0.1' IDENTIFIED BY RANDOM PASSWORD;
GRANT SELECT, INSERT, UPDATE, DELETE ON vanderclub.* TO 'vanderclub_api'@'127.0.0.1';

UPDATE schema_migrations SET estado='PRONTO', concluida_em=COALESCE(concluida_em, UTC_TIMESTAMP(6))
WHERE codigo='vanderclub-1.0.0';
DO RELEASE_LOCK('vanderclub_instalacao_v1');

-- 12. CONFERENCIA FINAL SOMENTE LEITURA
SELECT 'VanderClub: esquema 1.0.0 criado. Proxima etapa: API e bootstrap do Presidente.' AS resultado;
SELECT codigo, estado, concluida_em FROM schema_migrations;
SELECT COUNT(*) AS tabelas FROM information_schema.tables
WHERE table_schema='vanderclub' AND table_type='BASE TABLE';
SELECT COUNT(*) AS chaves_estrangeiras FROM information_schema.referential_constraints
WHERE constraint_schema='vanderclub';
SELECT COUNT(*) AS gatilhos FROM information_schema.triggers WHERE trigger_schema='vanderclub';
SELECT COUNT(*) AS estados FROM estados;
SHOW FULL TABLES;

/*
=============================================================================
CONTRATO PARA IMPLEMENTAR A API (estes comentarios nao executam comandos)
=============================================================================

1. PRIMEIRO ACESSO E CADASTROS
   - Bootstrap unico no servidor cria pessoa + usuario ADM_GERAL numa transacao.
     Use um password hasher robusto da plataforma, com salt e fator de trabalho.
     Nao use SHA2/MD5 simples nem uma senha publicada neste arquivo. Hash SHA-256
     de token aleatorio e apropriado para sessoes; nao equivale a hash de senha.
   - Cadastre cidades conforme a UF; nao inventamos 5.570 municipios/IBGE.
     Depois: unidades, administradores locais, documentos reais, planos e areas.
     A API resolve/cria cidade a partir de cidade+UF e relaciona sua FK.
   - Planos sao gerais: todos podem ser contratados em qualquer unidade ativa.
     LOCAL limita uso a clube_origem_id; REDE autoriza unidades elegiveis da rede.
     Nao existe tabela plano_versao_clubes: nao ha preco de catalogo por unidade.
   - Presidente e ADM Local sao usuarios.perfil. Vinculos locais ficam em
     administradores_clubes. Nao confundir perfil com membros_contrato.tipo.
   - TipoSocio 'Fundador' no cliente corresponde a TITULAR no banco.
     Os nomes/rotulos de DTO sao projetados pela API, nao usados como nomes SQL.

2. PLANOS E ADESAO
   - Edicao de oferta: bloquear planos.id FOR UPDATE; copiar a ultima versao
     para RASCUNHO com numero_versao+1, criar condicoes e publicar; tudo atomico.
     O numero e sequencial dentro do plano. Nunca alterar versao publicada.
   - Arquivar: planos.ativo=0; nao desativar contratos nem apagar versoes.
     A view publica escolhe a maior versao PUBLICADA. API exige que a adesao
     corresponda a essa versao atual; versao antiga recebida retorna 409.
   - Cada contrato referencia versao e condicao imutaveis. A view detalhada
     recupera nome, cotas, regras e beneficios antigos, mesmo apos edicao/arquivo.
   - Adesao: validar oferta/unidade/documento/aceite, identidade e cotas;
     criar pessoa, usuario, contrato, membro TITULAR, aceites e faturas na
     mesma transacao. Nunca marcar parcela paga durante cadastro.
   - Numero de contrato/protocolo: gerado no servidor (ex.: prefixo+UUID),
     nunca SELECT MAX(id)+1. Todos possuem restricao UNIQUE.
   - Gerar parcelas com divisao INTEIRA e distribuir o resto em centavos.
     Conferir quantidade e soma exata == contrato.valor_total_centavos.
     Para anual a vista: quantidade_parcelas=1. Datas civis, fim exclusivo.
   - Carencia: impedir reserva/uso antes de data_inicio+carencia_dias;
     nao mudar vigencia sem novo contrato. Um titular nao deve ter dois
     contratos vigentes sobrepostos sem regra explicita de migracao.

3. BENEFICIARIOS E MINHA CONTA
   - pessoa nao e usuario. Dependente pode existir sem login. Para conceder
     login, criar usuario ligado a mesma pessoa; nunca repetir CPF/pessoa.
   - Editar perfil atualiza pessoas; se email mudou, atualizar usuarios na
     mesma transacao. email_normalizado e derivado de TRIM/LOWER(email_login).
   - Validar CPF (digitos verificadores), idade, CEP e telefone na API.
     CPF e CEP sem pontuacao; nulo nao significa string vazia. Dados pessoais
     nunca retornam no chat/agenda publica nem fora do titular autorizado.
   - Interesses do DTO string: API converte entre pessoas_interesses e lista
     textual; nao gravar valores SQL concatenados. Todos os comandos parametrizados.
   - Cotas: bloquear contratos.id FOR UPDATE e contar membros PENDENTE+ATIVO
     (incluindo alteracoes pendentes) por categoria e no total antes de inserir.
     SUSPENSO tambem reserva vaga para impedir liberar cota por suspensao;
     REJEITADO/REMOVIDO nao ocupam. A regra deve ser igual em todos os endpoints.
   - Alteracao de beneficiario gera solicitacoes_vinculo com JSON proposto,
     coloca membro PENDENTE e revoga acesso ate nova aprovacao; a API deve
     verificar pendencia mesmo em sessoes emitidas anteriormente.
   - Aprovar/recusar exige administrador autorizado na unidade de origem,
     atualiza pessoa/membro/solicitacao e auditoria atomicamente. Recusa exige
     justificativa. Nunca permitir que o payload escolha aprovador ou perfil.
   - Remover = status REMOVIDO e revogar acesso. Historico preservado.
     Politicas de retencao/anonimizacao exigem rotina privilegiada especifica;
     o bloqueio de DELETE nao significa reter dados pessoais para sempre.

4. RESERVAS, EVENTOS E CONCORRENCIA (OBRIGATORIO)
   - ocupacoes_area e a agenda unica. Um CHECK de datas NAO impede duas
     transacoes reservarem o mesmo horario. Nao basta consultar disponibilidade
     no Mobile; usar este protocolo em TODAS as escritas, inclusive WEB:

       START TRANSACTION;
       SELECT id FROM areas WHERE id=:area_id FOR UPDATE;
       SELECT id FROM ocupacoes_area
       WHERE area_id=:area_id AND status='ATIVA'
         AND inicio_em < :novo_fim_com_intervalo
         AND TIMESTAMPADD(MINUTE,intervalo_apos_minutos,fim_em) > :novo_inicio
       FOR UPDATE;
       -- Se retornou qualquer linha: ROLLBACK e HTTP 409.
       -- Senao, inserir reserva + ocupacao + auditoria + resultado idempotente.
       COMMIT;

     :nomes sao parametros ilustrativos, nao SQL para colar no Workbench.
     Em edicao, excluir a propria ocupacao da consulta. Em mudanca de area,
     bloquear areas antiga/nova em ordem crescente de id para evitar deadlock.
     Eventos com varias areas bloqueiam todas na mesma ordem e transacao.
     A leitura FOR UPDATE e atual; nao usar snapshot antigo para validar conflito.
   - Intervalos [inicio,fim) aceitam reserva que comeca exatamente no fim
     da anterior se intervalo_apos_minutos=0. Cancelada nao ocupa agenda.
   - Validar inicio futuro, funcionamento/fuso IANA, capacidade, contrato
     vigente, abrangencia, cotas futuras, carencia e impedimentos do associado.
     A API converte cada dia local para UTC, inclusive viradas de data/fuso.
   - Tipos esportivos/jogos/academia <=180 min; saloes de festa/social usam
     areas.max_minutos=NULL (DTO Mobile representa como 0). Comparar segundos
     para nao aceitar 180 min e alguns segundos. Nao limitar saloes a 3 horas.
   - API copia regras_aceitas e prazo de cancelamento para reserva, preservando
     as regras daquele momento. O DTO atual nao informa quantidade: usar 1
     ate evoluir o formulario. Regras nao substituem capacidade real do evento.
   - horarios_area usa hora LOCAL, fim pode ser 24:00. Cruzou meia-noite?
     Dividir em duas faixas de dias adjacentes; validar sobreposicao das faixas.
     O DTO atual expoe apenas AbreMinuto/FechaMinuto: usar uma faixa diaria
     compativel ou evoluir o contrato antes de publicar horarios complexos.
   - Evento e diferente de reserva: eventos -> ocupacoes_area.tipo=EVENTO.
     Sincronizar periodo/status da ocupacao e do evento em uma transacao.
     Evento sem area nao bloqueia recurso. Cancelar libera ocupacao, sem DELETE.
     Estado Realizado e calculado apos fim_em, preservando Cancelado.
   - Confirmar/cancelar reserva tambem mantem reserva e ocupacao consistentes.
     A agenda publica so retorna intervalos, nunca nome/CPF de outro associado.

5. FINANCEIRO
   - Nao ha PIX/boleto/cartao real, gateway, webhook ativo ou cobranca automatica.
     Recebimento manual conferido e exclusivo do Presidente na API atual.
   - Bloquear fatura FOR UPDATE; conferir versao, status ABERTA, data nao futura
     e valor integral; inserir pagamento CONFIRMADO, quitacao_aplicada=1,
     conciliacao=REGULAR, referencia e autor, atualizar fatura QUITADA e
     quitada_em, registrar auditoria/idempotencia e COMMIT.
   - A UNIQUE de fatura_quitada_unica impede dois recebimentos aplicados.
     A API tambem confere montante/data/permissao: campos do cliente nao bastam.
   - Atrasada = ABERTA com vencimento anterior a data civil atual da unidade;
     vencendo hoje permanece Pendente. Cancelada nao e debito. Faturas QUITADA
     exigem recebimento aplicado na MESMA transacao. Sem job para mudar status.
   - Suspender = contratos.status=SUSPENSO, motivo/instante + auditoria. A API
     bloqueia uso e voto, preservando consulta de conta/debitos. Reativacao
     verifica todas as parcelas, fim do contrato e permissao administrativa.
   - Estornos/webhooks sao estrutura reservada. Nao habilitar endpoints sem
     implementar validacao, valor integral, reconciliacao e transacao de reabertura.

6. ATENDIMENTOS E CHAT
   - Chamado + primeira mensagem + auditoria sao criados na mesma transacao.
     Respostas inserem mensagens_chamado; nunca sobrescrever a descricao.
   - Solicitacao expoe status. Reclamacao conserva status interno para filas
     Web, sem exigir status na tela Mobile. respostaAdmin agrega historico
     legivel de respostas, autor/classe/data; respondidoUtc usa a ultima resposta.
   - Chat geral POR UNIDADE. Proibir escrita/leitura fora do escopo autorizado.
     Autor, nome e classe nas mensagens sao fotografias geradas no servidor.
     Apenas administradores fixam/moderam; texto simples, limite 2.000 caracteres.
     Nao executar HTML recebido. Ocultar mensagem preserva motivo/autor/data.
   - Paginar chat por id; API respeita limit/beforeId dos DTOs atuais. Polling
     e suficiente nesta versao; banco sozinho nao entrega atualizacoes push.

7. VOTACOES
   - Criar RASCUNHO, alternativas e publicar numa transacao; minimo 2 opcoes.
     AGENDADA/ABERTA/ENCERRADA sao transicoes feitas pela API/worker do servidor.
   - Na abertura: bloquear votacao FOR UPDATE, selecionar titulares ativos
     elegiveis da unidade de origem conforme data local/vigencia/adimplencia,
     inserir eleitores_votacao e marcar eleitorado_gerado_em, atomicamente.
     Nao regenerar a fotografia. Beneficiarios jamais sao eleitores.
   - Voto: bloquear votacao; conferir estado, periodo, eleitorado, contrato
     e elegibilidade atual; INSERT votos. UNIQUE garante um por pessoa na
     votacao por meio de eleitores_votacao(votacao_id,pessoa_id). As FK compostas
     impedem usar alternativa/eleitor de outra votacao.
   - Nunca retornar votos individuais ou relacao eleitor/opcao ao bootstrap.
     A view de resultados retorna contagens; API decide quando divulga-las,
     calcula participacao/quorum, percentual, empate e ausencia de votos.
   - Esta estrutura registra eleitor/opcao para integridade; NAO implementa
     voto secreto criptografico. Exigir sigilo forte implica arquitetura propria.

8. SESSOES, IDEMPOTENCIA, VERSAO E AUDITORIA
   - Mobile Bearer e Web cookie HttpOnly/Secure usam token aleatorio; somente
     hash de 32 bytes no banco. CSRF Web tem hash separado. Expirar/revogar
     e conferir usuario/associacao em toda requisicao. Sem refresh automatico.
   - usuarios.security_stamp permite invalidacao apos troca de senha/perfil;
     a API tambem revoga sessoes existentes na mesma transacao.
   - escopo_autenticacao = 'usuario:' + ID para autenticados. Cadastro publico
     usa 'adesao:' + HMAC servidor de identificador normalizado; nunca confiar
     no escopo enviado pelo cliente. usuario_id fica NULL antes da criacao.
   - Reservar a chave idempotente com INSERT e tratar colisao; conferir hash
     do payload, persistir resultado junto da mutacao atomica. Mesmo payload
     devolve mesmo resultado; diferente retorna 409. Expiracao com limpeza.
     Nao gravar senhas/tokens/cookies nem resposta de login nesta tabela.
   - Edicoes: UPDATE ... WHERE id=:id AND versao=:esperada; zero linhas =>409.
     O gatilho aumenta versao; ler valor atualizado na mesma transacao.
     Para alteracao de agregado (ex.: opcoes), conferir/bloquear pai e aumentar
     sua versao. Gatilho nao adivinha a versao esperada do cliente.
   - Auditoria de negocio e inserida pela API na mesma transacao. Gatilhos
     protegem o historico mas NAO geram logs automaticamente. Nao registrar
     hashes de senha, tokens, dados bancarios desnecessarios ou voto escolhido.
   - Conta SQL tecnica enxerga toda a base. Toda consulta precisa restringir
     ator/unidade/titular no servidor. Esconder botoes Web nao e autorizacao.
     Celular e navegador nunca recebem a connection string do banco.

9. MAPEAMENTO DOS MODULOS / PRONTO PARA A PROXIMA ETAPA
   Login/Administradores: pessoas, usuarios, administradores_clubes, sessoes.
   Unidades: clubes, cidades, estados. UF vem de cidades -> estados.
   Planos: planos, plano_versoes, condicoes_pagamento, documentos.
   Minha Conta: pessoas, pessoas_interesses, contratos, membros_contrato.
   Socios-convidados: membros_contrato, solicitacoes_vinculo, arquivos.
   Visitantes: visitantes. Eventos: eventos, ocupacoes_area.
   Reservas: tipos_area, areas, horarios_area, reservas, ocupacoes_area.
   Solicitacoes/Reclamacoes: chamados, mensagens_chamado.
   Chat: mensagens_chat, leituras_chat, moderacoes_chat, denuncias_chat.
   Votacoes: votacoes, opcoes_votacao, eleitores_votacao, votos.
   Mensalidades: contratos, faturas, pagamentos.
   Apoio: notificacoes, aceites_documentos, auditoria, requisicoes_idempotentes.
   Futuro: inscricoes_evento, estornos, eventos_provedor.

   IDs de usuario e pessoa NAO sao intercambiaveis: API resolve usuario.pessoa_id
   antes de consultar contrato/membro. DTO Associacao.UsuarioId aponta usuario;
   contratos.titular_pessoa_id aponta pessoa. Nao supor IDs numericos iguais.
   Valores, enums e nomes devem ser projetados no contrato API 2.0. Este SQL
   cria a estrutura; o Mobile e a Web ainda exigem a implementacao dessa API.
=============================================================================
*/
