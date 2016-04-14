FROM ubuntu:14.04
MAINTAINER Patrick Bédat <patrick.bedat@ixts.de>

RUN apt-key adv --keyserver keyserver.ubuntu.com --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
RUN echo "deb http://download.mono-project.com/repo/debian wheezy main" | tee /etc/apt/sources.list.d/mono-xamarin.list
## addtional for 12.04
RUN echo "deb http://download.mono-project.com/repo/debian wheezy-libtiff-compat main" | tee -a /etc/apt/sources.list.d/mono-xamarin.list

RUN apt-get update && \
	apt-get install -y \
	mono-complete

RUN mozroots --import --sync

COMMAND /app/azure-storage-bug.exe