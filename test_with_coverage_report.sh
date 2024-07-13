#!/bin/sh
# SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
# SPDX-License-Identifier: GPL-3.0-or-later
rm -rf CompactObliviousTransfer.Tests/TestResults/ && dotnet test --collect:"XPlat Code Coverage" ; reportgenerator -reports:CompactObliviousTransfer.Tests/TestResults/*/coverage.cobertura.xml -targetdir:coveragereport -reporttypes:Html
