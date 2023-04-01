using System;
using static System.Console;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Nethereum.HdWallet;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using NBitcoin;
using Rijndael256;

namespace Wallets
{
    class EthereumWallet
    {
        // sử dụng mạng goerli (test net )với api key của bản thân 
        //Goerli là một mạng thử nghiệm(testnet) của Ethereum được sử dụng phổ biến nhất bởi các nhà phát triển Ethereum 
        //    trên toàn thế giới và được Etherum Foundation hỗ trợ.
        const string network = "https://goerli.infura.io/v3/0bbdba2727b94dbc8a6aed7e973b0c01"; 
        const string workingDirectory = @"Wallets\"; // Path where you want to store the Wallets

        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            //các hành động được thực hiện ban đầu .
            string[] availableOperations =
            {
                "create", "load", "recover", "exit" 
            };
            string input = string.Empty;
            bool isWalletReady = false;
            // tạo ví wallet với từ vựng tiếng anh , và 12 chữ cái 
            Wallet wallet = new Wallet(Wordlist.English, WordCount.Twelve);

            // tạo Web3 instant với network đã chọn
           
            Web3 web3 = new Web3(network);
            Directory.CreateDirectory(workingDirectory);

            while (!input.ToLower().Equals("exit"))
            {
                if (!isWalletReady) // nếu user chưa có wallet
                {
                    do
                    {
                        input = ReceiveCommandCreateLoadOrRecover();

                    } while (!((IList)availableOperations).Contains(input));
                    switch (input)
                    {
                        // Tạo ví mới với Phrase gợi nhớ và public, private key.
                        case "create":
                            wallet = CreateWalletDialog();
                            isWalletReady = true;
                            break;

                        // Load ví từ json file chứ đoạn phrase gợi nhớ đã mã hóa .
                        // câu lệnh này sẽ giải mã csc từ và load ví .
                        case "load":
                            wallet = LoadWalletDialog();
                            isWalletReady = true;
                            break;

                        /* lấy lại ví đã quên bằng phrase gợi nhớ mà người dùng phải nhập vào   */
                        case "recover":
                            wallet = RecoverWalletDialog();
                            isWalletReady = true;
                            break;

                        // thoát chương trình
                        case "exit":
                            return;
                    }
                }
                else 
                {
                    // nếu đã có load được  ví chúng ta sẽ có những hành động :
                    // kiểm tra tài khoản , gửi và nhận . 
                    

                    string[] walletAvailableOperations = {
                        "balance", "receive", "send", "exit" 
                    };

                    string inputCommand = string.Empty;

                    while (!inputCommand.ToLower().Equals("exit"))
                    {
                        do
                        {
                            inputCommand = ReceiveCommandForEthersOperations();

                        } while (!((IList)walletAvailableOperations).Contains(inputCommand));
                        switch (inputCommand)
                        {
                            // Gửi transaction từ  address tới address
                            case "send":
                                await SendTransactionDialog(wallet);
                                break;

                            // Hiện ra số dư của các địa chỉ coin  và tổng của chúng .
                            case "balance":
                                await GetWalletBallanceDialog(web3, wallet);
                                break;

                            // Hiện ra những địa chỉ mà bạn có thể dùng để nhận coin .
                            case "receive":
                                Receive(wallet);
                                break;
                            case "exit":
                                return;
                        }
                    }
                }

            }
        }
        // Provided Dialogs.
        private static Wallet CreateWalletDialog()
        {
            try
            {
                string password;
                string passwordConfirmed;
                do
                {
                    Write("Enter password for encryption: ");
                    password = ReadLine();
                    Write("Confirm password: ");
                    passwordConfirmed = ReadLine();
                    if (password != passwordConfirmed)
                    {
                        WriteLine("Passwords did not match!");
                        WriteLine("Try again.");
                    }
                } while (password != passwordConfirmed);

                // gọi hàm tạo ví mới với mật khẩu 
                Wallet wallet = CreateWallet(password, workingDirectory);
                return wallet;
            }
            catch (Exception)
            {
                WriteLine($"ERROR! Wallet in path {workingDirectory} can`t be created!");
                throw;
            }
        }
        private static Wallet LoadWalletDialog()
        {
            Write("Enter: Name of the file containing wallet: ");
            string nameOfWallet = ReadLine();
            Write("Enter: Password: ");
            string pass = ReadLine();
            try
            {
                // Loading ví từ JSON file. sử dụng  Password để giải mã.
                Wallet wallet = LoadWalletFromJsonFile(nameOfWallet, workingDirectory, pass);
                return (wallet);

            }
            catch (Exception e)
            {
                WriteLine($"ERROR! Wallet {nameOfWallet} in path {workingDirectory} can`t be loaded!");
                throw e;
            }
        }
        private static Wallet RecoverWalletDialog()
        {
            try
            {
                Write("Enter: Mnemonic words with single space separator: ");
                string mnemonicPhrase = ReadLine();
                Write("Enter: password for encryption: ");
                string passForEncryptionInJsonFile = ReadLine();

                // từ phrase gợi nhớ , load ví đã quên 
                Wallet wallet = RecoverFromMnemonicPhraseAndSaveToJson(
                    mnemonicPhrase, passForEncryptionInJsonFile, workingDirectory);
                return wallet;
            }
            catch (Exception e)
            {
                WriteLine("ERROR! Wallet can`t be recovered! Check your mnemonic phrase.");
                throw e;
            }
        }
        private static async Task GetWalletBallanceDialog(Web3 web3, Wallet wallet)
        {
            WriteLine("Balance:");
            try
            {
                // Lấy số dư của từng địa chỉ của ví và tổng của chúng 
                await Balance(web3, wallet);
            }
            catch (Exception)
            {
                WriteLine("Error occured! Check your wallet.");
            }
        }
        private static async Task SendTransactionDialog(Wallet wallet)
        {
            WriteLine("Enter: Address sending ethers.");
            string fromAddress = ReadLine();
            WriteLine("Enter: Address receiving ethers.");
            string toAddress = ReadLine();
            WriteLine("Enter: Amount of coins in ETH.");
            double amountOfCoins = 0d;
            try
            {
                amountOfCoins = double.Parse(ReadLine());
            }
            catch (Exception)
            {
                WriteLine("Unacceptable input for amount of coins.");
            }
            if (amountOfCoins > 0.0d)
            {
                WriteLine($"You will send {amountOfCoins} ETH from {fromAddress} to {toAddress}");
                WriteLine($"Are you sure? yes/no");
                string answer = ReadLine();
                if (answer.ToLower() == "yes")
                {
                    // Send the Transaction.
                    await Send(wallet, fromAddress, toAddress, amountOfCoins);
                }
            }
            else
            {
                WriteLine("Amount of coins for transaction must be positive number!");
            }
        }
        private static string ReceiveCommandCreateLoadOrRecover()
        {
            WriteLine("Choose working wallet.");
            WriteLine("Choose [create] to Create new Wallet.");
            WriteLine("Choose [load] to load existing Wallet from file.");
            WriteLine("Choose [recover] to recover Wallet with Mnemonic Phrase.");
            Write("Enter operation [\"Create\", \"Load\", \"Recover\", \"Exit\"]: ");
            string input = ReadLine().ToLower().Trim();
            return input;
        }
        private static string ReceiveCommandForEthersOperations()
        {
            Write("Enter operation [\"Balance\", \"Receive\", \"Send\", \"Exit\"]: ");
            string inputCommand = ReadLine().ToLower().Trim();
            return inputCommand;
        }

        // TODO: Implement this methods.

        public static Wallet CreateWallet(string password, string pathfile)
        {
            // TODO:Tạo ví với 12 từ tiếng anh

            Wallet wallet = new Wallet(Wordlist.English, WordCount.Twelve);
            string words = string.Join(" ", wallet.Words);
            string fileName = string.Empty;

            try
            {
                // TODO: Lưu ví vô thư mục  
                fileName = SaveWalletToJsonFile(wallet, password, pathfile);
            }
            catch (Exception e)
            {
                WriteLine($"ERROR! The file can`t be saved! {e}");
                throw e;
            }

            WriteLine("New Wallet was created successfully!");
            WriteLine("Write down the following mnemonic words and keep them in the save place.");
            // TODO: hiện ra phrase gợi nhớ 
            WriteLine(words);
            WriteLine("Seed: ");
            // TODO: hiện ra  Seed .
            WriteLine(wallet.Seed);
            WriteLine();
            // TODO: In ra toàn bộ các địa chỉ và khóa .
            PrintAddressesAndKeys(wallet);

            return wallet;
        }
        private static void PrintAddressesAndKeys(Wallet wallet)
        {
            // TODO: hiện ra 20 địa chỉ và khóa tương ứng
            WriteLine("Addresses: ");
            for (int i = 0; i < 20; i++)
            {
                WriteLine(wallet.GetAccount(i).Address);
            }

            WriteLine();
            WriteLine("Private Keys: ");
            for (int i = 0; i < 20; i++)
            {
                WriteLine(wallet.GetAccount(i).PrivateKey);
            }

            WriteLine();
        }
        private static string SaveWalletToJsonFile(Wallet wallet, string password, string pathfile)
        {
            //TODO: Mã hóa và lưu ví dạng json.
            string words = string.Join(" ", wallet.Words);
            var encryptedWords = Rijndael.Encrypt(words, password, KeySize.Aes256);
            string date = DateTime.Now.ToString();
            var walletJsonData = new { encryptedWords = encryptedWords, date = date };
            string json = JsonConvert.SerializeObject(walletJsonData);
            Random random = new Random();
            var fileName =
                "EthereumWallet_"
                + DateTime.Now.Year + "-"
                + DateTime.Now.Month + "-"
                + DateTime.Now.Day + "-"
                + DateTime.Now.Hour + "-"
                + DateTime.Now.Minute + "-"
                + DateTime.Now.Second + "-"
                + random.Next(0, 1000) + ".json";
            File.WriteAllText(Path.Combine(pathfile, fileName), json);
            WriteLine($"Wallet saved in file: {fileName}");
            return fileName;
        }

        static Wallet LoadWalletFromJsonFile(string nameOfWalletFile, string path, string pass)
        {
            //TODO: Logic để load ví từ file  JSON.
            string pathToFile = Path.Combine(path, nameOfWalletFile);
            string words = string.Empty;
            WriteLine($"Read from {pathToFile}");
            try
            {
                string line = File.ReadAllText(pathToFile);
                dynamic results = JsonConvert.DeserializeObject<dynamic>(line);
                string encryptedWords = results.encryptedWords;
                words = Rijndael.Decrypt(encryptedWords, pass, KeySize.Aes256);
                string dataAndTime = results.date;
            }
            catch (Exception e)
            {
                WriteLine("ERROR!" + e);
            }

            return Recover(words);
        }
        public static Wallet Recover(string words)
        {
            // TODO:Phục hồi lại ví từ phrase gợi nhớ (words).
            Wallet wallet = new Wallet(words, null);
            WriteLine("Wallet was successfully recovered.");
            WriteLine("Words: " + string.Join(" ", wallet.Words));
            WriteLine("Seed: " + string.Join(" ", wallet.Seed));
            WriteLine();
            PrintAddressesAndKeys(wallet);
            return wallet;
        }

        public static Wallet RecoverFromMnemonicPhraseAndSaveToJson(string words, string password, string pathfile)
        {
           // TODO: Phục hồi lại ví từ các từ gợi nhớ và lưu vào json file .
           Wallet wallet = Recover(words);
           string fileName = string.Empty;
           try
           {
               fileName = SaveWalletToJsonFile(wallet, password, pathfile);
           }
           catch (Exception)
           {
               WriteLine($"ERROR! The file {fileName} with recovered wallet can't be saved!");
               throw;
           }

           return wallet;
        }

        public static void Receive(Wallet wallet)
        {
            // TODO: hiện ra tất cả các địa chỉ của ví 
            if (wallet.GetAddresses().Count() > 0)
            {
                for (int i = 0; i < 20; i++)
                {
                    WriteLine(wallet.GetAccount(i).Address);
                }
                WriteLine();
            }
            else
            {
                WriteLine("No addresses found!");
            }
        }
        static async Task Send(Wallet wallet, string fromAddress, string toAddress, double amountOfCoins)
        {
            // TODO: lấy account từ địa chỉ nguoofn và gửi tới địa chỉ đích.
            Account accountFrom = wallet.GetAccount(fromAddress);
            string privateKeyFrom = accountFrom.PrivateKey;
            if (privateKeyFrom == string.Empty)
            {
                WriteLine("Address sending coins is not from current wallet!");
                throw new Exception("Address sending coins is not from current wallet!");
            }
            // chuyển số lượng tiền gửi qua wei
            var web3 = new Web3(accountFrom, network);
            var wei = Web3.Convert.ToWei(amountOfCoins);
            try
            {
                var transaction = await web3.TransactionManager.SendTransactionAsync(
                    accountFrom.Address,
                    toAddress,
                    new Nethereum.Hex.HexTypes.HexBigInteger(wei)
                );
                WriteLine("Transaction has been sent successfully!");
            }
            catch (Exception e)
            {
                WriteLine($"ERROR! The transaction can't be completed! {e}");
                throw e;
            }
        }
        static async Task Balance(Web3 web3, Wallet wallet)
        {
            // TODO: In ra toàn bộ số dư từ các địa chỉ của ví và in ra tổng số coin 
            decimal totalBalance = 0.0m;
            for (int i = 0; i < 20; i++)
            {
                var balance = await web3.Eth.GetBalance.SendRequestAsync(wallet.GetAccount(i).Address);
                var etherAmount = Web3.Convert.FromWei(balance.Value);
                totalBalance += etherAmount;
                WriteLine(wallet.GetAccount(i).Address + " " + etherAmount + " ETH");
            }

            WriteLine($"Total balance: {totalBalance} ETH \n");
        }
      }
    }
  
