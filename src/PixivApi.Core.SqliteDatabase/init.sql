BEGIN TRANSACTION;
CREATE TABLE "InfoTable" (
    "Major" INTEGER NOT NULL DEFAULT 0,
    "Minor" INTEGER NOT NULL DEFAULT 0,
    UNIQUE ("Major", "Minor")
);

INSERT INTO "InfoTable" VALUES (0, 1);

CREATE TABLE "UserTable" (
    "Id" INTEGER NOT NULL PRIMARY KEY,
    "Name" TEXT NULL COLLATE BINARY,
    "Account" TEXT NULL COLLATE BINARY,
    "IsFollowed" INTEGER NOT NULL DEFAULT 0,
    "IsMuted" INTEGER NOT NULL DEFAULT 0,
    "IsOfficiallyRemoved" INTEGER NOT NULL DEFAULT 0,
    "HideReason" INTEGER NOT NULL DEFAULT 0,
    "ImageUrls" TEXT NULL COLLATE BINARY,
    "Comment" TEXT NULL COLLATE BINARY,
    "Memo" TEXT NULL COLLATE BINARY,
    "HasDetail" INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX "UserTable_IsFollowed_Index" ON "UserTable" ("IsFollowed");
CREATE INDEX "UserTable_IsOfficiallyRemoved_Index" ON "UserTable" ("IsOfficiallyRemoved");
CREATE INDEX "UserTable_HideReason_Index" ON "UserTable" ("HideReason");

CREATE TABLE "UserDetailTable" (
    "Id" INTEGER NOT NULL PRIMARY KEY REFERENCES "UserTable" ("Id"),
    "Profile_Webpage" TEXT NULL COLLATE BINARY,
    "Profile_Gender" TEXT NULL COLLATE BINARY,
    "Profile_Birth" TEXT NULL COLLATE BINARY,
    "Profile_BirthYear" INTEGER NOT NULL DEFAULT 0,
    "Profile_BirthDay" TEXT NULL COLLATE BINARY,
    "Profile_Region" TEXT NULL COLLATE BINARY,
    "Profile_AddressId" INTEGER NOT NULL DEFAULT 0,
    "Profile_CountryCode" TEXT NULL COLLATE BINARY,
    "Profile_Job" TEXT NULL COLLATE BINARY,
    "Profile_JobId" INTEGER NOT NULL DEFAULT 0,
    "Profile_TotalFollowUsers" INTEGER NOT NULL DEFAULT 0,
    "Profile_TotalIllusts" INTEGER NOT NULL DEFAULT 0,
    "Profile_TotalManga" INTEGER NOT NULL DEFAULT 0,
    "Profile_TotalNovels" INTEGER NOT NULL DEFAULT 0,
    "Profile_TotalIllustBookmarksPublic" INTEGER NOT NULL DEFAULT 0,
    "Profile_TotalIllustSeries" INTEGER NOT NULL DEFAULT 0,
    "Profile_TotalNovelSeries" INTEGER NOT NULL DEFAULT 0,
    "Profile_BackgroundImageUrl" TEXT NULL COLLATE BINARY,
    "Profile_TwitterAccount" TEXT NULL COLLATE BINARY,
    "Profile_TwitterUrl" TEXT NULL COLLATE BINARY,
    "Profile_PawooUrl" TEXT NULL COLLATE BINARY,
    "Profile_IsPremium" INTEGER NOT NULL DEFAULT 0,
    "Profile_IsUsingCustomProfileImage" INTEGER NOT NULL DEFAULT 0,
    "ProfilePublicity_Gender" TEXT NULL COLLATE BINARY,
    "ProfilePublicity_Region" TEXT NULL COLLATE BINARY,
    "ProfilePublicity_BirthDay" TEXT NULL COLLATE BINARY,
    "ProfilePublicity_BirthYear" TEXT NULL COLLATE BINARY,
    "ProfilePublicity_Job" TEXT NULL COLLATE BINARY,
    "ProfilePublicity_Pawoo" INTEGER NOT NULL DEFAULT 0,
    "Workspace_Pc" TEXT NULL COLLATE BINARY,
    "Workspace_Monitor" TEXT NULL COLLATE BINARY,
    "Workspace_Tool" TEXT NULL COLLATE BINARY,
    "Workspace_Scanner" TEXT NULL COLLATE BINARY,
    "Workspace_Tablet" TEXT NULL COLLATE BINARY,
    "Workspace_Mouse" TEXT NULL COLLATE BINARY,
    "Workspace_Printer" TEXT NULL COLLATE BINARY,
    "Workspace_Desktop" TEXT NULL COLLATE BINARY,
    "Workspace_Music" TEXT NULL COLLATE BINARY,
    "Workspace_Desk" TEXT NULL COLLATE BINARY,
    "Workspace_Chair" TEXT NULL COLLATE BINARY,
    "Workspace_Comment" TEXT NULL COLLATE BINARY,
    "Workspace_WorkspaceImageUrl" TEXT NULL COLLATE BINARY
);

CREATE TABLE "ArtworkTable" (
    "Id" INTEGER NOT NULL PRIMARY KEY,
    "UserId" INTEGER NOT NULL REFERENCES "UserTable" ("Id"),
    "PageCount" INTEGER NOT NULL DEFAULT 0,
    "Width" INTEGER NOT NULL DEFAULT 0,
    "Height" INTEGER NOT NULL DEFAULT 0,
    "Type" INTEGER NOT NULL DEFAULT 0,
    "Extension" INTEGER NOT NULL DEFAULT 0,
    "IsXRestricted" INTEGER NOT NULL DEFAULT 0,
    "IsVisible" INTEGER NOT NULL DEFAULT 0,
    "IsMuted" INTEGER NOT NULL DEFAULT 0,
    "CreateDate" TEXT NOT NULL COLLATE BINARY,
    "FileDate" TEXT NOT NULL COLLATE BINARY,
    "TotalView" INTEGER NOT NULL DEFAULT 0,
    "TotalBookmarks" INTEGER NOT NULL DEFAULT 0,
    "HideReason" INTEGER NOT NULL DEFAULT 0,
    "IsOfficiallyRemoved" INTEGER NOT NULL DEFAULT 0,
    "IsBookmarked" INTEGER NOT NULL DEFAULT 0,
    "Title" TEXT NULL COLLATE BINARY,
    "Caption" TEXT NULL COLLATE BINARY,
    "Memo" TEXT NULL COLLATE BINARY
);

CREATE INDEX "ArtworkTable_IsXRestricted_Index" ON "ArtworkTable" ("IsXRestricted");
CREATE INDEX "ArtworkTable_ArtworkTotalBookmarks_Index" ON "ArtworkTable" ("TotalBookmarks");
CREATE INDEX "ArtworkTable_ArtworkIsOfficiallyRemoved_Index" ON "ArtworkTable" ("IsOfficiallyRemoved");
CREATE INDEX "ArtworkTable_ArtworkIsBookmarked_Index" ON "ArtworkTable" ("IsBookmarked");
CREATE INDEX "ArtworkTable_ArtworkHideReason_Index" ON "ArtworkTable" ("HideReason");
CREATE INDEX "ArtworkTable_UserId_Index" ON "ArtworkTable" ("UserId");

CREATE VIRTUAL TABLE "ArtworkTextTable" USING fts5(
    "Title",
    "Caption",
    "Memo",
    tokenize="trigram case_sensitive 1",
    content="ArtworkTable",
    content_rowid="Id"
);

CREATE TRIGGER "Trigger_Add_ArtworkTable" AFTER INSERT ON "ArtworkTable"
BEGIN
    INSERT INTO "ArtworkTextTable"("rowid", "Title", "Caption", "Memo") VALUES ("new"."Id", "new"."Title", "new"."Caption", "new"."Memo");
END;

CREATE TRIGGER "Trigger_Update_ArtworkTable" AFTER UPDATE ON "ArtworkTable"
BEGIN
    INSERT INTO "ArtworkTextTable"("ArtworkTextTable", "rowid", "Title", "Caption", "Memo") VALUES ('delete', old."Id", old."Title", old."Caption", old."Memo");
    INSERT INTO "ArtworkTextTable"("rowid", "Title", "Caption", "Memo") VALUES ("new"."Id", "new"."Title", "new"."Caption", "new"."Memo");
END;

CREATE TABLE "TagTable" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, 
    "Value" TEXT NOT NULL UNIQUE COLLATE BINARY
);
CREATE TABLE "ToolTable" (
    "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    "Value" TEXT NOT NULL UNIQUE COLLATE BINARY
);

CREATE UNIQUE INDEX "TagIndex" ON "TagTable" ("Value");
CREATE UNIQUE INDEX "ToolIndex" ON "ToolTable" ("Value");

CREATE VIRTUAL TABLE "TagTextTable" USING fts5(
    "Value",
    tokenize="trigram case_sensitive 1"
);

CREATE TRIGGER "Trigger_Add_TagTable" AFTER INSERT ON "TagTable"
BEGIN
    INSERT INTO "TagTextTable"("rowid", "Value") VALUES ("new"."Id", "new"."Value");
END;

CREATE VIRTUAL TABLE "ToolTextTable" USING fts5(
    "Value",
    tokenize="trigram case_sensitive 1",
    content="ToolTable",
    content_rowid="Id"
);

CREATE TRIGGER "Trigger_Add_ToolTable" AFTER INSERT ON "ToolTable"
BEGIN
    INSERT INTO "ToolTextTable"("rowid", "Value") VALUES ("new"."Id", "new"."Value");
END;

CREATE TABLE "ArtworkTagCrossTable" (
    "Id" INTEGER NOT NULL REFERENCES "ArtworkTable" ("Id"),
    "TagId" INTEGER NOT NULL REFERENCES "TagTable" ("Id"),
    "ValueKind" INTEGER NOT NULL DEFAULT 1,
    PRIMARY KEY ("Id", "TagId")
);

CREATE INDEX "ArtworkTagCrossTable_Id_Index" ON "ArtworkTagCrossTable" ("Id");
CREATE INDEX "ArtworkTagCrossTable_TagId_Index" ON "ArtworkTagCrossTable" ("TagId");

CREATE TABLE "ArtworkToolCrossTable" (
    "Id" INTEGER NOT NULL REFERENCES "ArtworkTable" ("Id"),
    "ToolId" INTEGER NOT NULL REFERENCES "ToolTable" ("Id"),
    PRIMARY KEY ("Id", "ToolId")
);

CREATE INDEX "ArtworkToolCrossTable_Id_Index" ON "ArtworkToolCrossTable" ("Id");
CREATE INDEX "ArtworkToolCrossTable_ToolId_Index" ON "ArtworkToolCrossTable" ("ToolId");

CREATE TABLE "UserTagCrossTable" (
    "Id" INTEGER NOT NULL REFERENCES "UserTable" ("Id"),
    "TagId" INTEGER NOT NULL REFERENCES "TagTable" ("Id"),
    "ValueKind" INTEGER NOT NULL DEFAULT 1,
    PRIMARY KEY ("Id", "TagId")
);

CREATE INDEX "UserTagCrossTable_Id_Index" ON "UserTagCrossTable" ("Id");

CREATE TABLE "UgoiraFrameTable" (
    "Id" INTEGER NOT NULL REFERENCES "ArtworkTable" ("Id"),
    "Index" INTEGER NOT NULL DEFAULT 0,
    "Delay" INTEGER NOT NULL DEFAULT 0
);

CREATE INDEX "UgoiraFrameTable_Id_Index" ON "UgoiraFrameTable" ("Id");
CREATE INDEX "UgoiraFrameTable_Index_Index" ON "UgoiraFrameTable" ("Index");

CREATE TABLE "HidePageTable" (
    "Id" INTEGER NOT NULL REFERENCES "ArtworkTable" ("Id"),
    "Index" INTEGER NOT NULL DEFAULT 0,
    "HideReason" INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY ("Id", "Index")
);

CREATE INDEX "HidePageTable_Id_Index" ON "HidePageTable" ("Id");

CREATE TABLE "RankingTable" (
    "Date" TEXT NOT NULL COLLATE BINARY,
    "RankingKind" INTEGER NOT NULL DEFAULT 0,
    "Index" INTEGER NOT NULL DEFAULT 0,
    "Id" INTEGER NOT NULL REFERENCES "ArtworkTable" ("Id"),
    PRIMARY KEY ("Date", "RankingKind", "Index")
);

CREATE INDEX "RankingTable_Date_RankingKind_Index" ON "RankingTable" ("Date", "RankingKind");

CREATE TABLE "ArtworkRemoveTable"(
    "Id" INTEGER NOT NULL PRIMARY KEY REFERENCES "ArtworkTable" ("Id")
);

CREATE TABLE "UserRemoveTable"(
    "Id" INTEGER NOT NULL PRIMARY KEY REFERENCES "UserTable" ("Id")
);
END TRANSACTION;