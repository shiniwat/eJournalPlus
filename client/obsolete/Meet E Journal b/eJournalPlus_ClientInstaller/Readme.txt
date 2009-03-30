///
/// some caveat for getting eJournalPlus client built and run
///
eJournalPlus has external dependency on Windows Live ID modules.
In order for getting external components installed, you have to do the following steps:

1. Go to MS download center (http://www.microsoft.com/downloads/details.aspx?FamilyID=b5a78784-922d-4267-a6e9-5d2ecf1dced8&displaylang=en).

2. download two merge modules (idcrlWix2.msm, mgdidcrl.msm), and put them under the following folder:
	<root>\client\Meet E Journal b\eJournalPlus_ClientInstaller\redist

3. download SDK itself (WLIDClientSDK.msi), and install it.

4. Some project (WlsBridge, in particular) has explicit reference to "Microsoft.WindowsLive.Id.Client" assembly.
You may have to resolve the reference by yourselves by removing the reference once and then re-adding it.

Please use codeplex discussion for any questions. Thanks.
