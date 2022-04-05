CREATE DATABASE `OCPPCore` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;
USE `OCPPCore`;
CREATE TABLE `ChargePoint` (
                               `Id` int unsigned NOT NULL,
                               `ChargePointId` varchar(50) NOT NULL,
                               `Name` varchar(100) NOT NULL,
                               `Comment` varchar(200) DEFAULT NULL,
                               `Username` varchar(50) NOT NULL,
                               `Password` varchar(50) NOT NULL,
                               `ClientCertThumb` varchar(100) DEFAULT NULL,
                               PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `ChargeTags` (
                              `Id` int NOT NULL AUTO_INCREMENT,
                              `TagId` varchar(50) NOT NULL,
                              `TagName` varchar(200) DEFAULT NULL,
                              `ParentTagId` varchar(45) DEFAULT NULL,
                              `ExpiryDate` datetime DEFAULT NULL,
                              `Blocked` tinyint(1) DEFAULT '0',
                              PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `ConnectorStatus` (
                                   `Id` int NOT NULL AUTO_INCREMENT,
                                   `ChargePointId` int unsigned NOT NULL,
                                   `ConnectorId` int NOT NULL,
                                   `ConnectorName` varchar(100) DEFAULT NULL,
                                   `LastStatus` varchar(100) DEFAULT NULL,
                                   `LastStatusTime` datetime DEFAULT NULL,
                                   `LastMeter` float DEFAULT NULL,
                                   `LastMeterTime` datetime DEFAULT NULL,
                                   PRIMARY KEY (`Id`),
                                   KEY `clustered_key` (`ChargePointId`,`ConnectorId`),
                                   CONSTRAINT `fk_cp_cp_cpid` FOREIGN KEY (`ChargePointId`) REFERENCES `ChargePoint` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE `MessageLog` (
                              `LogId` int NOT NULL AUTO_INCREMENT,
                              `LogTime` datetime NOT NULL,
                              `ChargePointId` int unsigned NOT NULL,
                              `ConnectorId` int DEFAULT NULL,
                              `Message` varchar(100) NOT NULL,
                              `Result` longtext,
                              `ErrorCode` varchar(100) DEFAULT NULL,
                              PRIMARY KEY (`LogId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE TABLE `Transactions` (
                                `TransactionId` int NOT NULL AUTO_INCREMENT,
                                `Uid` varchar(45) DEFAULT NULL,
                                `ChargePointId` int unsigned NOT NULL,
                                `ConnectorId` int NOT NULL,
                                `StartTagId` varchar(45) NOT NULL,
                                `StartTime` datetime NOT NULL,
                                `MeterStart` float NOT NULL,
                                `StartResult` varchar(100) NOT NULL,
                                `StopTagId` varchar(45) NOT NULL,
                                `StopTime` datetime DEFAULT NULL,
                                `MeterStop` float DEFAULT NULL,
                                `StopReason` varchar(100) NOT NULL,
                                PRIMARY KEY (`TransactionId`),
                                KEY `fk_tx_cp_cpid_idx` (`ChargePointId`),
                                CONSTRAINT `fk_tx_cp_cpid` FOREIGN KEY (`ChargePointId`) REFERENCES `ChargePoint` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
CREATE ALGORITHM=UNDEFINED DEFINER=`root`@`%` SQL SECURITY DEFINER VIEW `ConnectorStatusView` AS select `cs`.`ChargePointId` AS `ChargePointId`,`cs`.`ConnectorId` AS `ConnectorId`,`cs`.`ConnectorName` AS `ConnectorName`,`cs`.`LastStatus` AS `LastStatus`,`cs`.`LastStatusTime` AS `LastStatusTime`,`cs`.`LastMeter` AS `LastMeter`,`cs`.`LastMeterTime` AS `LastMeterTime`,`t`.`TransactionId` AS `TransactionId`,`t`.`StartTagId` AS `StartTagId`,`t`.`StartTime` AS `StartTime`,`t`.`MeterStart` AS `MeterStart`,`t`.`StartResult` AS `StartResult`,`t`.`StopTagId` AS `StopTagId`,`t`.`StopTime` AS `StopTime`,`t`.`MeterStop` AS `MeterStop`,`t`.`StopReason` AS `StopReason` from (`ConnectorStatus` `cs` left join `Transactions` `t` on(((`t`.`ChargePointId` = `cs`.`ChargePointId`) and (`t`.`ConnectorId` = `cs`.`ConnectorId`)))) where ((`t`.`TransactionId` is null) or `t`.`TransactionId` in (select max(`Transactions`.`TransactionId`) AS `Expr1` from `Transactions` group by `Transactions`.`ChargePointId`,`Transactions`.`ConnectorId`));
