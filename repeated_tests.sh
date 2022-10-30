#!/bin/sh
# SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
# SPDX-License-Identifier: GPL-3.0-or-later
while [ 1 ]; do dotnet test; if [ $? -ne 0 ]; then break; fi; done
