CREATE TABLE Accounts (
    Id bigint NOT NULL,
    FirstName varchar(50) NOT NULL,
    FullName varchar(50) NOT NULL,
    Email varchar(50) NOT NULL,
    CONSTRAINT PK_1_ACCOUNTS PRIMARY KEY (Id)
);

CREATE TABLE Videos (
    Id int NOT NULL,
    Title varchar(50) NOT NULL,
    Description varchar(50),
    Category int NULL,
    OwnerAccountID bigint NOT NULL,
    Views int NOT NULL,
    CONSTRAINT PK_1_VIDEOS PRIMARY KEY (Id),
    CONSTRAINT FK_1_VIDEOS FOREIGN KEY (OwnerAccountID) REFERENCES Accounts (Id),
);

CREATE INDEX FK_1_VIDEOS ON Videos (OwnerAccountID);

CREATE TABLE Likes (
    Id int NOT NULL,
    Video int NOT NULL,
    Account bigint NOT NULL,
    Unlike boolean NOT NULL,
    CONSTRAINT PK_1_LIKES PRIMARY KEY (Id),
    CONSTRAINT FK_6_LIKES FOREIGN KEY (Account) REFERENCES Accounts (Id),
    CONSTRAINT FK_7_LIKES FOREIGN KEY (Video) REFERENCES Videos (Id)
);

CREATE INDEX FK_1_LIKES ON Likes (Account);

CREATE INDEX FK_2_LIKES ON Likes (Video);

CREATE TABLE Comments (
    Id int NOT NULL,
    VideoID int NOT NULL,
    Data varchar(2000) NOT NULL,
    CommenterID bigint NOT NULL,
    CONSTRAINT PK_1_COMMENTS PRIMARY KEY (Id),
    CONSTRAINT FK_3_COMMENTS FOREIGN KEY (VideoID) REFERENCES Videos (Id),
    CONSTRAINT FK_4_COMMENTS FOREIGN KEY (CommenterID) REFERENCES Accounts (Id)
);

CREATE INDEX FK_1_COMMENTS ON Comments (VideoID);

CREATE INDEX FK_3_COMMENTS ON Comments (CommenterID);