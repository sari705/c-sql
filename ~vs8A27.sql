/*SELECT Id, Title, Description, DueDate, Status, IsRecurring FROM Tasks*/

ALTER TABLE Tasks
ADD CONSTRAINT DF_IsRecurring DEFAULT 0 FOR IsRecurring;
UPDATE Tasks
SET IsRecurring = 0
WHERE IsRecurring IS NULL;
SELECT Id, Title, Description, DueDate, Status, IsRecurring
FROM Tasks;
SELECT Id, Title, IsRecurring FROM Tasks;

CREATE TABLE Statuses (
    StatusId INT PRIMARY KEY IDENTITY(1,1), -- מפתח ראשי עם אינדקס ייחודי אוטומטי
    StatusName NVARCHAR(100) NOT NULL      -- שם הסטטוס
);

INSERT INTO Statuses (StatusName) VALUES ('Pending');
INSERT INTO Statuses (StatusName) VALUES ('Completed');
INSERT INTO Statuses (StatusName) VALUES ('In Progress');





