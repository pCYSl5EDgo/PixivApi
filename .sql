CREATE TABLE `UserTable` (
    `Id` INTEGER NOT NULL PRIMARY KEY,
    `Name` TEXT NULL COLLATE BINARY,
    `Account` TEXT NULL COLLATE BINARY,
    `IsFollowed` INTEGER NOT NULL,
    `IsMuted` INTEGER NOT NULL,
    `IsOfficiallyRemoved` INTEGER NOT NULL,
    `HideReason` INTEGER NOT NULL,
    `ImageUrls` TEXT NULL COLLATE BINARY,
    `Comment` TEXT NULL COLLATE BINARY,
    `Memo` TEXT NULL COLLATE BINARY,
    `DetailProfile_WebPage` TEXT NULL COLLATE BINARY,
    `DetailProfile_Gender` TEXT NULL COLLATE BINARY,
    `DetailProfile_Birth` TEXT NULL COLLATE BINARY,
    `DetailProfile_BirthYear` INTEGER NOT NULL DEFAULT 0,
    `DetailProfile_BirthDay` TEXT NULL COLLATE BINARY,
    `DetailProfile_Region` TEXT NULL COLLATE BINARY,
    `DetailProfile_AddressId` INTEGER NOT NULL DEFAULT 0,
    `DetailProfile_CountryCode` TEXT NULL COLLATE BINARY,
    `DetailProfile_Job` TEXT NULL COLLATE BINARY,
    `DetailProfile_JobId` INTEGER NOT NULL DEFAULT 0,
    `DetailProfile_TotalFollowUsers` INTEGER NOT NULL DEFAULT 0,
    `DetailProfile_TotalIllusts` INTEGER NOT NULL DEFAULT 0,
    `DetailProfile_TotalManga` INTEGER NOT NULL DEFAULT 0,
    `DetailProfile_TotalNovels` INTEGER NOT NULL DEFAULT 0,
    `DetailProfile_TotalIllustBookmarksPublic` INTEGER NOT NULL DEFAULT 0,
    `DetailProfile_TotalIllustSeries` INTEGER NOT NULL DEFAULT 0,
    `DetailProfile_TotalNovelSeries` INTEGER NOT NULL DEFAULT 0,
    `DetailProfile_BackgroundImageUrl` TEXT NULL COLLATE BINARY,
    `DetailProfile_TwitterAccount` TEXT NULL COLLATE BINARY,
    `DetailProfile_TwitterUrl` TEXT NULL COLLATE BINARY,
    `DetailProfile_PawooUrl` TEXT NULL COLLATE BINARY,
    `DetailProfile_IsPremium` INTEGER NULL,
    `DetailProfile_IsUsingCustomProfileImage` INTEGER NULL,
    `DetailProfilePublicity_Gender` TEXT NULL COLLATE BINARY,
    `DetailProfilePublicity_Region` TEXT NULL COLLATE BINARY,
    `DetailProfilePublicity_BirthDay` TEXT NULL COLLATE BINARY,
    `DetailProfilePublicity_BirthYear` TEXT NULL COLLATE BINARY,
    `DetailProfilePublicity_Job` TEXT NULL COLLATE BINARY,
    `DetailProfilePublicity_Pawoo` INTEGER NULL,
    `DetailWorkspace_Pc` TEXT NULL COLLATE BINARY,
    `DetailWorkspace_Monitor` TEXT NULL COLLATE BINARY,
    `DetailWorkspace_Tool` TEXT NULL COLLATE BINARY,
    `DetailWorkspace_Scanner` TEXT NULL COLLATE BINARY,
    `DetailWorkspace_Tablet` TEXT NULL COLLATE BINARY,
    `DetailWorkspace_Mouse` TEXT NULL COLLATE BINARY,
    `DetailWorkspace_Printer` TEXT NULL COLLATE BINARY,
    `DetailWorkspace_Desktop` TEXT NULL COLLATE BINARY,
    `DetailWorkspace_Music` TEXT NULL COLLATE BINARY,
    `DetailWorkspace_Desk` TEXT NULL COLLATE BINARY,
    `DetailWorkspace_Chair` TEXT NULL COLLATE BINARY,
    `DetailWorkspace_Comment` TEXT NULL COLLATE BINARY,
    `DetailWorkspace_WorkspaceImageUrl` TEXT NULL COLLATE BINARY
) STRICT;

CREATE INDEX `UserIsFollowedIndex` ON `UserTable` (`IsFollowed`);
CREATE INDEX `UserIsOfficiallyRemovedIndex` ON `UserTable` (`IsOfficiallyRemoved`);
CREATE INDEX `UserHideReasonIndex` ON `UserTable` (`HideReason`);

CREATE TABLE `ArtworkConcreteTable` (
    `Id` INTEGER NOT NULL PRIMARY KEY,
    `UserId` INTEGER NOT NULL REFERENCES `UserTable` (`Id`),
    `PageCount` INTEGER NOT NULL,
    `Width` INTEGER NOT NULL,
    `Height` INTEGER NOT NULL,
    `Type` INTEGER NOT NULL,
    `Extension` INTEGER NOT NULL,
    `IsXRestricted` INTEGER NOT NULL,
    `IsVisible` INTEGER NOT NULL,
    `IsMuted` INTEGER NOT NULL,
    `CreateDate` TEXT NOT NULL COLLATE BINARY,
    `FileDate` TEXT NOT NULL COLLATE BINARY
) STRICT;

CREATE INDEX `IsXRestrictedIndex` ON `ArtworkConcreteTable` (`IsXRestricted`);

CREATE TABLE `ArtworkSoftTable` (
    `Id` INTEGER NOT NULL PRIMARY KEY REFERENCES `ArtworkConcreteTable` (`Id`),
    `TotalView` INTEGER NOT NULL,
    `TotalBookmarks` INTEGER NOT NULL,
    `HideReason` INTEGER NOT NULL,
    `IsOfficiallyRemoved` INTEGER NOT NULL,
    `IsBookmarked` INTEGER NOT NULL,
    `Title` TEXT NOT NULL COLLATE BINARY,
    `Caption` TEXT NOT NULL COLLATE BINARY,
    `Memo` TEXT NULL COLLATE BINARY
) STRICT;

CREATE INDEX `ArtworkTotalBookmarksIndex` ON `ArtworkSoftTable` (`TotalBookmarks`);
CREATE INDEX `ArtworkIsOfficiallyRemovedIndex` ON `ArtworkSoftTable` (`IsOfficiallyRemoved`);
CREATE INDEX `ArtworkIsBookmarkedIndex` ON `ArtworkSoftTable` (`IsBookmarked`);
CREATE INDEX `ArtworkHideReasonIndex` ON `ArtworkSoftTable` (`HideReason`);

CREATE VIRTUAL TABLE `ArtworkTextTable` USING fts5(
    `Title`,
    `Caption`,
    `Memo`,
    tokenize="trigram",
    content=`ArtworkSoftTable`,
    content_rowid=`Id`
);

CREATE TABLE `TagTable` (
    `Id` INTEGER NOT NULL PRIMARY KEY, 
    `VALUE` TEXT NOT NULL UNIQUE ON CONFLICT IGNORE COLLATE BINARY
) STRICT;
CREATE TABLE `ToolTable` (
    `Id` INTEGER NOT NULL PRIMARY KEY,
    `VALUE` TEXT NOT NULL UNIQUE ON CONFLICT IGNORE COLLATE BINARY
) STRICT;

CREATE UNIQUE INDEX `TagIndex` ON `TagTable` (`VALUE`);
CREATE UNIQUE INDEX `ToolIndex` ON `ToolTable` (`VALUE`);

CREATE TABLE `ArtworkTagCrossTable` (
    `ArtworkId` INTEGER NOT NULL REFERENCES `ArtworkConcreteTable` (`Id`),
    `TagId` INTEGER NOT NULL REFERENCES `TagTable` (`Id`),
    `ValueKind` INTEGER NOT NULL
) STRICT;

CREATE TABLE `ArtworkToolCrossTable` (
    `ArtworkId` INTEGER NOT NULL REFERENCES `ArtworkConcreteTable` (`Id`),
    `ToolId` INTEGER NOT NULL REFERENCES `ToolTable` (`Id`),
    `ValueKind` INTEGER NOT NULL
) STRICT;

CREATE TABLE `UserTagCrossTable` (
    `UserId` INTEGER NOT NULL REFERENCES `UserTable` (`Id`),
    `TagId` INTEGER NOT NULL REFERENCES `TagTable` (`Id`),
    `ValueKind` INTEGER NOT NULL
) STRICT;

CREATE TABLE `UgoiraFrameTable` (
    `ArtworkId` INTEGER NOT NULL REFERENCES `ArtworkConcreteTable` (`Id`),
    `Index` INTEGER NOT NULL,
    `Delay` INTEGER NOT NULL,
    PRIMARY KEY (`ArtworkId`, `Index`)
) STRICT;

CREATE TABLE `HidePageTable` (
    `ArtworkId` INTEGER NOT NULL REFERENCES `ArtworkConcreteTable` (`Id`),
    `Index` INTEGER NOT NULL,
    `HideReason` INTEGER NOT NULL,
    PRIMARY KEY (`ArtworkId`, `Index`)
) STRICT;

CREATE TABLE `RankingTable` (
    `Date` TEXT NOT NULL COLLATE BINARY,
    `RankingKind` INTEGER NOT NULL,
    `ArtworkId` INTEGER NOT NULL REFERENCES `ArtworkConcreteTable` (`Id`)
) STRICT;