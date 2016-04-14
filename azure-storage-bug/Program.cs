using System;
using System.IO;
using Microsoft.WindowsAzure.Storage;
using System.Collections.Generic;

namespace windowsstoragebug
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			if (args.Length < 2)
			{
				Console.WriteLine ("usage: <account-name> <account-key>");
				return;
			}

			var accountName = args [0];
			var accountKey = args [1];

			var file = Path.GetTempFileName ();

			var blobId = Path.GetFileName (file);

			var containerId = "container" + Guid.NewGuid ().ToString ().Replace("-", "");

			var account = new CloudStorageAccount (new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials (accountName, accountKey), true);

			var blobClient = account.CreateCloudBlobClient();
			blobClient.DefaultRequestOptions.ServerTimeout = new TimeSpan (1, 0, 0);
			blobClient.DefaultRequestOptions.MaximumExecutionTime = new TimeSpan (1, 0, 0);
			blobClient.DefaultRequestOptions.SingleBlobUploadThresholdInBytes = 67108864; //64M

			try{

				Console.WriteLine ("creating a 500MB file: {0}", file);

				File.WriteAllBytes (file, new byte[1024 * 1024 * 500]);

				Console.WriteLine ("creating container {0}", containerId);

				var container = blobClient.GetContainerReference(containerId);

				container.CreateIfNotExists();


				var blob = container.GetBlockBlobReference(Path.GetFileName(file));

				Console.WriteLine ("uploading (UploadFromFile)...");

				blob.UploadFromFile (file);

				Console.WriteLine ("upload complete!");

			}
			catch(Exception ex) 
			{
				Console.WriteLine ("ERROR:\n" + ex.ToString());
			}
			finally
			{				
				blobClient.GetContainerReference (containerId).DeleteIfExists ();
			}

			try{
				containerId = "container" + Guid.NewGuid ().ToString ().Replace("-", "");
				var container = blobClient.GetContainerReference(containerId);

				container.CreateIfNotExists();

				var blob = container.GetBlockBlobReference(Path.GetFileName(file));

				blob.UploadFromByteArray (new byte[0], 0, 0);
				blob.DeleteIfExists ();

				blob = container.GetBlockBlobReference(Path.GetFileName(file));

				Console.WriteLine ("uploading (PutBlock approach)...");

				using(var stream = File.OpenRead(file))
				{
					int position = 0;
					const int BLOCK_SIZE = 4 * 1024 * 1024;
					int currentBlockSize = BLOCK_SIZE;

					var blockIds = new List<string>();
					var blockId = 0;

					while(currentBlockSize == BLOCK_SIZE)
					{
						if ((position + currentBlockSize) > stream.Length)
							currentBlockSize = (int)stream.Length - position;
						byte[] chunk = new byte[currentBlockSize];
						stream.Read (chunk, 0, currentBlockSize);

						Console.WriteLine (currentBlockSize);

						var base64BlockId = Convert.ToBase64String(System.Text.Encoding.Default.GetBytes(blockId.ToString("d5")));

						using(var memoryStream = new MemoryStream(chunk))
						{
							memoryStream.Position = 0;
							Console.WriteLine ("PutBlock {0}", base64BlockId);						
							blob.PutBlock(base64BlockId, memoryStream, null);
						}


						blockIds.Add(base64BlockId);

						position += currentBlockSize;
						blockId++;

						Console.WriteLine ("progress: {0}%", position / (double)stream.Length * 100);

					}

					Console.WriteLine ("Committing");

					blob.PutBlockList(blockIds);
				}

				Console.WriteLine ("upload complete!");

			}
			catch(Exception ex) 
			{
				Console.WriteLine ("ERROR:\n" + ex.ToString());
			}
			finally
			{				
				blobClient.GetContainerReference (containerId).DeleteIfExists ();
				File.Delete (file);
			}
		}
	}
}
