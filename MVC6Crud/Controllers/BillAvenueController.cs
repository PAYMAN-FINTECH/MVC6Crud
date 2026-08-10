using Microsoft.AspNetCore.Mvc;
using MVC6Crud.Models;
using MVC6Crud.Models.PaymanApp;
using MVC6Crud.Models.PaymanWeb;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Security.Policy;
using System.Text;
using System.Xml.Serialization;

namespace MVC6Crud.Controllers
{
    public class BillAvenueController : Controller
    {
        public IActionResult Index()
        {
           //var shfjh = FetchBillAsync();
//            string workingKey = "831CA4627BD384B4112540F9E9ED0296";
//            string merchantData = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
//<billerInfoRequest>
//  <billerId>OU12BB000NATKB</billerId>
//</billerInfoRequest>";

            //            string rr = "ceb174c994fa4cf2521af06a89b61113dcaae968c0bee693bc0159594afbac350b2c99c072820df3a786abcfada2724b32f12437c1fc4394efd12902b9f38614cda0a077200e9656fb0281b6fb200a0c851bd68c73076ea76b5b53f3b8c6200aee4375a6b9ddf261ac793cfc9b37acd647cf7a12882c644194b4caa925bde27761a61235d32ab36322060232404365dbc7189cdca8d899c8e12efcdb10ae4f3cc9e5fffced46d20871c0f943f09a9f6f8523d19ccfe60ab693e6fbad969d0daf9ec862963bf995f72bbca5ae8a27f85139009cae75e34cb74d4b7b0c6ec44e548c7be0b448846319d9c1ac6e1b57b0645284aada8baa8a8a6075c2fa5b56a17edd625941b6cfccc48252927e02636bf9f58aab65f10f7b29246d36d337feddd6238c35a3ac7a737abe46d3e12c4dd59125b02dd146a7a2630f3bfa4884cdd787134c89b523755e1cd71e262b145b8b45ec8393070ba2d8ab89817999a614e8dccb49b6c0b8c0d86a7df8c0cdab9b6040badfe10b7ae8767a5cd41f9a7d6f97998b1b5c3db6a5f035d5becc7ab7a26e7de15b7f374f9b6c7f719c8297f0dc96eac4c9ed5d9d93a3c1a9dfaf19b0c3d7724ea4f1d25091aa343a24edb7f9f7f544b6626980e340126822f1e737398199387ba940982c6aa8dc0d965ae7cc9d75ccedbcd2b47a73be66e33e1e5a3c9c7aec1cbe474621ea5effe856a9cb421cb093023bf7d7fc6ce480249d03f67ffde61703b588b205e76586e367aa6e415fcf66aacb5b1777d82fe3361b30e0731a5dc4f259d576cbbf414f4dbc4122d299d8993d85b259dd9d408429ef8dcf4186f7d99323caf2c028b4dc4640c6e1d59b52c2f416febdc3f0b66a486156d92b333ebfd95195ac3086c4630a52317b7083f311cffe5c354157f028de24b228474e2eb0b97f2ae55d5995ed39d4d096fd8238580b3bdc0b2f82b59db4e051ee5d1cf551e43c754dc53bc79d9d38453ccdb42a07aa786ccc48e41f9fede56a43b3b1807db38b4aa3e250c9cd348c14b155311ffcab3474070015144fe71a45dfbd7e5fe39f38d3f4f1c9300b50de7dd4bafc6544f11733ffdf0d55b8326fcbf950bc50d5b8d219cd2a25f407beaff4d30939c705d2bc8ea251ff349e923aea07ba7e537c92341a89406fa7b185c6e4eb788c91c91619d131bf797530eb9f30b2744193d933e4a9b19512ebdc0b6cb2efa75868bad964e3c15c272d68412ae683b9e83d708b82b131dba926d39546bf8f68ac568c9bd66e339b085c86bcfaee02693c79a9489e48b210a89905e3d95dfd82087a738bc79cf952bd023e2a85aab1430c6358e3d18eb75d5fc6681018427a5348a108637d0b36feed77ba8f89b2ac2e250a5e65424c02eae40c3bfa0cca52fe67e17842484c87eae73513831ff52932d4c5a75766bc5a321ff610cca1abfa92bb6a9d1fef2494bbc3de78befc61371dfc8c2ce62eb2e78c557fc6a4cc79485f91c0df0e711da8c3d79af83e8ae9b652e3bf37a20544c943b987302a2ba033b735a58bb6d04fe38a718d671c4722985fef438bbda802eb75c5ac6dd982a0d5c526d38b98cc7ef39ae2b99d058c97aed62a3094214d28b229ef87709c144277eb7e6e05676837cea6468e7c6ad233f923463d892fc1b7117c4fd451b3bc4b24fb0f4bc9f9fb6b0a8848c0a1f16b845950339435cf4b78ef20543c299eff98cc8e9a94f0930e5f08e96d077afcc8569db93aa92d24b70d859d7332cd2d0b8fbfa5c4cc87d380d49020cb6171932908fc77911ed206eec5a4b5335d1a830b5727e349142f082aa28c48d2c8ce0e62bcbb5c4f7cb6b553e0f30361a5debbc243f015290eae60fdbe84208b6c634c277584d4fa80bfd8ceac707434e5da4c4db05425907a333f36d742dd211b397bf31229b0743865477a1e7669875df769261d1b48e5c0087b20cfdce5c06aef8f3cd749a70e75f29463a4dbdf70732fd89aaadd7aa82d8a04992a1bc9bf204c2be8231acfe341a420fba6d27c561374af498b1088f7a47fdfa4c9796344110a5acef7115ea055925b1845129929ac09dfba219f8e8fa7eb752130a03b20926259f08a2513eb6ae550075db4b85fc68b9941dc967badd6acf703a6ea4d1a754ceae99da0940e76e5c4ee4634cbbe2ca311d2eb6469538fa9a14f0feee85fe53d3506e8ee406e6ec6ed61ae2218f18919b9f24b5e971851c46f2ca9c904a416be5ab103fbd30f8fc9e7476067bc014f6804ea31ae216f37ae299957fa603588dbdd8c62667bf57f3e6ec3fe59a8deb090d8f3d85badcb027f62e1332a5489f0da291279f586fe230d77228d9c559b8b5aaf936e20857caa8fe06311cc5f8419478bb6d000db45e6b406b28c341f150efb90ebe003684750d2f7473b1c8124ac156fecc20337a525cb8d40b14cb34042b9d9842177024be5099564f03e438838a1eafb26265d3452f69c6d922565d49fa102464cc610d4e95f5f5e51e0b9c43c4e1bc2f7abe91387caa87c2352c85a2504012822bdcdf352af3508334c896064dde184c24a70ddf89f6c7d24692bf90d93f97934f3c695e515aa402058855a4f4949f40e302323a80b52512b37e36b0e9635821a94a8173a73b0d57cb404f6659cac52ca1ce61b41426ba601feb80b8e6081c665e9343101968d7d7bab4f4a32249dc0fa8a0e7ef62e9684a0d920a1781442928f6b7e3dc9b91fe849769a0fad553d8d03eba64a378e006893017f760fa41c38f07858193aca40f27cc3d9713e20928256726bdbaf3da1f995b27a54529915d98ac82b5d780a7918656a1b7ed09d7309cd8dc45163dab68a7b41985744422a81b550430ab57d1d96caf10205d70633ac65c5a2381b8a544756628113dbdad0a965361de371a05007de32242110e973b8a793be106445fefd3814fdf56481866fe02c6b9241717b66103132a39f2577e12993278149e7e5d9bc545c09445c3a12e647fa8531179827a0261a56e6a3a7a6f2ff7742f8f44a8acb274ba411ef62a188f8e14c1050ca11333c2b2e8d1de67c8f65e7277ef59834ef615c8ca73fcf935473fc946b021efe8ca829e22d46a23f10f728b78d2e53cfb7571ae64852e445bd4db48cd75b7b3490bc46fc26352242bcd818ec953f79835c1b2b32a8b13078719c35e4347e926a9abb76a3955d273ee557e110bcf89504a1866689401f2d021ad05269986b8ab32464e7aafda7b31458f7ca5655d6664c09e3174cc1ce35102c36c0c6dd117b1ee72a8fa7104e1f1b68780a362835adbde0643849f97dbc74d47d13078ed7189b8b357b7da33d5829224ca7de4aba8ea69126a25dd4d19aa91190c1c8c6a5f2b8942a95992f299861f3b73bb3c61928b0204c1068de30a17e6258bd28d175badf2aae5493259cdee965f994cd321982e93b73461bd2568db64667553a57d49b3b10b9d605ae5fc3ac590371a4bf6c2995d93643c528b7837478cf85edadebe8ab97f809b2bb9ced93b77977d3c06470c06b9f976c58b63911a67fd6f1e466e93081eed21ab2ae0f0fd334b31569be4204c43b9a64a9fe6f302f246bfd807b176f834c37774d0d099735aca7324ce74672a38a1e60c60b575d8164b8f19552c69d8b0590a37b7ebde4ab463b89749bf6e8c77308db7d1f9d5f6085ae967ab3d915fbb4ec15cc6cb5b553ef69fa9f7b264b1fd06f41f13be9121f790d68d57954de24049c0591bacbb77528e8ce58410b0a9d9267eec6e15d86bf591307c14ed31d71230dcae1f99fce5094ed36cda200ae54fbaffb794c88bd5e4883a056f9ecbba10cfc0bc310ca8f10b01ad0e359656d8ca0e999a876259d3d02efe78faa681340278d5635ecad6f368e27eb508193142494fa59aa0f9db61689ebfca9cced18a9dc5b240e6ce6910ab5e61a2a6cec1dbc5992d7ae6154f87d5511bfc26289825047b8624e58e20a56ffd94464cac3fedadfed6857203c42a2bbd06ed644e98dc01385af42feaf268f2b923e70dd429a5443272173aa8d4ae8d0acd332b0d65d4a271028b9ba940080cf7001c79ee428e6454fe79ffff4963ecc020e05fb8ab53920c7292c00fee9946a95acfefd0da6dad02cefb640f4459994053c1d8b9bbc32bd96fca3c7085c166996ed1131d1ed3197c8019ad5aa9fd53c729d326146d680fd8e3b0d33235e64a17ec4df8bfafa3984c2484c2c11bf9a55ffed1f0e1dc933255ccf43a75b00ee05cce285b5d74ca0b4bf5f717c81bd8179b9dd762d2f47aa0bb6f861fee5c985f2631c97fd3e732415dbf3f325ef6c3ff1be8d33ebc5bf1e86692a2a76236ffa3824f999937d207eaa9508404110cdbd3b7b3157df318738c3742e28574cbe6ca3f55a3db61098f1cae8afeb77cce7b208f0af76b0bdba2fc7b11d5a77d4e6a314de106ad7197075b2dfe36272964eabb65f799b527e7220f6dbcc61324e34f87e043d1334f5cfcc08de3343170fcd3028efedd488a19b0013511ec8a1bcbd447c40e8e8cf1ab76f70f6ba9baa2b6ee0e30b24314aa3f6aa226f44365e911d426b927e536c4a22d865b7df7415fe5e7e2fecfb2fd6db5f85d10ddc24cdc25a05e8ca356c57f28006948316fe027d7a1a5c7d10900f24c012fa57497ab3142f7fbf51b5ca1ccd7d2e6e3b6dba098c25459b71ecaf4e0949fb1ae1e0e5d20ddf7826a5638cf720a1de818047db839e640848e53651b434b1dd912e49d7eb8be01697a3442cd8ff8f9469017de2a8596449374a56cfedbf92d4feca04bec3c36832c992f90116497cd4baf980e93803570d6eea7abb0d55ba6d4975d66b248333dba2d8d5b33f417deaba03b7e2ac92c5333cd7a4346687a27966395271ad170fed2627e31061a0feed821064afbc288e23620e88b17492faa877a1710cecb10976fbfb4afcdbc6008cfd3dee9f88853d84e4dfc40d621378d19af27d5963afaaf4795faca7fade193115746fc1462419079df800e651a536cac1a06badecaed1a3fb1d63db73bac7d97db2a95adf6d282e296b47c1b77f98ca7f3ce233f47786c336d8c51fc116a5dc1a21a45f72f549354f1d5590882e81ca761db439f6ef12f975f35083357f8ba20adb546c9da963c54c0d23cc903b1137c2a4a184b87af3fef99da7276cb0a3843982c051192112dcbaedb67dc32256eb0b04a6a602ea263e62a64c3e43937dd9a3dab59f1b5211ff53565c45e31df8bd2c37a332b5719f60fa45745bd5fb53b88aa74dd4bed438f97dd51dd8870ed1ffe026476535f5be20dd481923c0327d2676ef7d6c607ee3c897bdf703f2f353b8271948ea783a97dc242d2e67fe22574c021042bb3bb3cf2ccd1257162fe6defcf7f70cd9de6cabf1ef4f2cac4cd465fbe22937cfe1e59043259b17f58d5edda1118b5d8d5bf2c5e667a046e90dded51aa7b60b0dc6d5efbcb2a000b60b9ad5bea50d14dbfb905adb53c3558777a4dae5f7ce090e016ed9a26214b1f0402c043c1b58cdda190d095ab512e049f55c281e8334ec07c518c9bff444c89618bae768a2faa9fd70452fcc0e46d58eeecdc8a73b7da2476dcc486d49d7b9443100c8b323c8dc48309ba9526e52e65fa577957196f292760ce383c226930df8479714dbef77fe53547d79e89a09d60c83743bcf7e09ee11767b81408ca220d109a35276d59f5cc4f25b7693cf22573655fa0d3c5d7026179f152db5058ab98e345417c64ef8e11070945cb554f26debdb9be4668bd8a82825b5abb142820d35e148847cbe238a096d4621d939975a2efd07b9e7a1d9d0fe5703bc088d0cb6509a74a6caa478a9c5afb6ebb901a5a159071e1f91f2a14d9ccda200b0b05328b6812324c3f4ba0fa44e6ecb75776d23fbd3e985385f53d5aa4df208b2e23f012d880bd06de0702ba8988b39551866056906fd3f9afdf9c70011a5b46eefa93b361461dc45411453d1cd9cfcb5aba506f0bb9d7043aab64c044d9541efb702e203433bf5d78084bb1beb792b5c1e11091f2d67707244de188a12e441215a0a14d5091e1dd7e0056f2149ee00627321da288524ff363095da1e29a0c4184bb982078155ecf4016bc2d40889b8b0e6bd171a5e39451eb5df91038d3636e27899be986260cd63d25d7edc741753875c5b8a1aa33de3281e9e33b9a2344ed3b0846413142bd541815442ad7db75bf8eb62e252b822f52ed716aa265d5ab623091d5669cf41422acbf65933fb771f4b8567aafcf1f79118467e496974cc70a6abd118bc90d06368a79ae1490a3445d1da202d12b17b349c54d0056704d5bbc234c9759b0236995db6b3985da5c88f7aed2d8961b5319b274c27f293642e3c33a9c32178e765a23db1ef6d2d40d9af6709282d7ccc54c78b8cc2ba099dcd753528fa2c57d7e22c43a671f7ef448f259fd770c329415df28c5eff5726d3f0679bc48309a469b8674ad0dbd1b68bbc16c0c11ea096118fdf3e9422c7282cddb22d3851f20d129f3d2d894b96751090ddd219860db5117b2216825483626efa46fda6e4a1f32f959e8f9d372c80ea297ecec0070dca0d8c57aa7368f422f45b92a99f822059c6556db9af8a1b592621bdcc1789630764ac7544aa3892099e456531d45e9ea414e32f1d4b9c456768d61432e494616024a5dc80d321927e613527790df538d340e110cc478551a5c00079ed4c19243f7670767b5746af16121be55c279a00be0afc3221669f17457e6edeba6a7192d69c5857ccf4c0054b71871e863fc73629777d82fdced3b3e67a9c2b7c2a26310ca6fbd9424a4eeaacc7bbe0a2d547376a1cd077a3a69d91c101017e96e99c3017da5808731053a58e8e402587405d1f861bccf5971af953b626dcac4ea910f6732001b990a0068c2923c3d7b5c8d0d776985b5825cc78369a54252ef8aaa3cd8d4cbb8751badfd258005fc6dccaadacd4dca3a862f79145ace8baec1e5615bd1149eb678dce73f43206ffcecb02b496c261f068b6aea77efa662bb52021c1dd6ea1f4d0aea67aaac43e8539bac9ddd0a21689c111db39d69aae7c7eff3ccd6bad85a7876e2e1cc381213335a138560f518c5659d3c4647b1ce9599689369df42d91981ba1c75392def0cd65d732c5d5e74dc56ce415ab4ef3e314f6a14e36828a574e0921d014ee6cda0033a67cfb8c285ae762e068c33425556bca45ffd1989a111ac2b1780ddb61da12a51948618126171ffe8540321f14fd0c422a3e46c9805dcec8cab1ac66182841b620b1c87a64ca1655966758c49dec41105434cbf21926f57b467fd3244e52be757c5a25845be2d2a17d03d2a80916c6960ce211a7a1a896d657771ebe2b5ab360b25c97bf77eece76890f7c03b596bd46fb548b12039cf8111ab4471b0887181edb97ff5acb0153f62c3c1065e37725e9dcbbf4c22236210b938cd88e4725ddea37e855e70cc201ee07a32a1fd1f74e44f14a6ad1bdf2";

            //            string decryptedData = Decrypt(rr, workingKey);
            //string encrypted = Encrypt(merchantData, workingKey);
            //  ViewBag.Encrypted = encrypted;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> FetchBill(string billerId, string mobile, string customerNumber, string last4)
        {
            string accessCode = "AVIV40NP24LP89LAJG";
            string requestId = GenerateRequestId();
            string workingKey = "831CA4627BD384B4112540F9E9ED0296";
            string ver = "1.0";
            string instituteId = "PF06";
            string merchantData = $@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?> 
<billFetchRequest>
    <agentId>CC01CC01513515340681</agentId>
    <agentDeviceInfo>
        <ip>192.168.2.73</ip>
        <initChannel>AGT</initChannel>
        <mac>01-23-45-67-89-ab</mac>
    </agentDeviceInfo>
    <customerInfo>
        <customerMobile>9898990854</customerMobile>
        <customerEmail></customerEmail>
        <customerAdhaar></customerAdhaar>
        <customerPan></customerPan>
    </customerInfo>
    <billerId>OU12BB000NATKB</billerId>
    <inputParams>
        <input>
            <paramName>Registered Mobile Number</paramName>
            <paramValue>7378926240</paramValue>
        </input>
        <input>
            <paramName>Last 4 digits of Credit Card Number</paramName>
            <paramValue>1864</paramValue>
        </input>
    </inputParams>
</billFetchRequest>
";
            string encryptedPayload = Encrypt(merchantData, workingKey);

            var url = $"https://stgapi.billavenue.com/billpay/extBillCntrl/billFetchRequest/xml" +
                  $"?accessCode={accessCode}&requestId={requestId}&ver={ver}&instituteId={instituteId}&encRequest={encryptedPayload}";



            using (var client = new HttpClient())
            {
                try
                {
                    var content = new StringContent(encryptedPayload, Encoding.UTF8, "text/plain"); // Body is raw text
                    var response = await client.PostAsync(url, null);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                    }


                    var result = await response.Content.ReadAsStringAsync();

                    string decryptedResponse = Decrypt(result, workingKey);
                }
                catch (HttpRequestException ex)
                {
                }
            }






            var billData = new
            {
                amount = "450.00",
                customerName = "Raj Kumar",
                billNumber = "BILL789456",
                dueDate = "2025-08-15",
                status = "Pending"
            };

            return Json(billData);
        }


        [HttpPost]
        public IActionResult ProcessPayment([FromBody] BillAvenuePaymentRequest request)
        {
            // Simulate processing logic (you can replace this with real logic/API call)
            if (string.IsNullOrWhiteSpace(request.BillerId) || request.Amount <= 0)
            {
                return BadRequest(new { message = "Invalid payment data." });
            }
            var response = new
            {
                isSuccess = true,
                billerName = "Credit Card",
                mobileNumber = "9849800697",
                billNumber = "BILL123456",
                billDate = DateTime.Now.ToString("dd/MM/yyyy"),
                dueDate = DateTime.Now.AddDays(7).ToString("dd/MM/yyyy"),
                transactionId = "TXN4202526",
                registeredMobile = "9849800697",
                amount = request.Amount.ToString("0.00"),
                dateTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                message = "Success"
            };

            return Json(response);
        }



        public async Task<IActionResult> GetBillerInfoAsync(string billerId)
        {
            string accessCode = "AVIV40NP24LP89LAJG";
            string requestId = GenerateRequestId();
            string workingKey = "831CA4627BD384B4112540F9E9ED0296";
            string ver = "1.0";
            string instituteId = "PF06";
            string merchantData = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<billerInfoRequest>
  <billerId>OU12BB000NATKB</billerId>
</billerInfoRequest>";

            string apiUrl = $"https://stgapi.billavenue.com/billpay/extMdmCntrl/mdmRequestNew/xml" +
                            $"?accessCode={accessCode}&requestId={requestId}&ver={ver}&instituteId={instituteId}";

            string encryptedPayload = Encrypt(merchantData, workingKey);

            using (var client = new HttpClient())
            {
                try
                {
                    var content = new StringContent(encryptedPayload, Encoding.UTF8, "text/plain"); // Body is raw text
                    var response = await client.PostAsync(apiUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        // 🧩 Decrypt the response
                        string decryptedResponse = Decrypt(result, workingKey);
                        ViewBag.Response = decryptedResponse;
                        ViewBag.ActiveTab = "biller";

                        // Optionally return as XML or plain text
                        return View("Index");
                    }
                    else
                    {
                        return View("Index");
                    }
                }
                catch (Exception ex)
                {
                    return View("Index");
                }
            }
        }

        public async Task<string> FetchBillAsync()
        {
            string accessCode = "AVIV40NP24LP89LAJG";
            string requestId = GenerateRequestId();
            string workingKey = "831CA4627BD384B4112540F9E9ED0296";
            string ver = "1.0";
            string instituteId = "PF06";
            string merchantData = $@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?> 
<billFetchRequest> 
<agentDeviceInfo> 
<app>tripozo</app> 
<imei>000000000000000</imei> 
<initChannel>MOB</initChannel> 
<ip>103.108.7.127</ip> 
<os>android</os> 
</agentDeviceInfo> 
<agentId>CC01CC01513515340681</agentId> 
<billerId>POCK00000NATDZ</billerId> 
<customerInfo> 
<customerEmail>divyeshsachan@gmail.com</customerEmail> 
<customerMobile>8004480444</customerMobile> 
</customerInfo> 
<inputParams> 
<input> 
<paramName>MOBILE NUMBER</paramName> 
<paramValue>8004480444</paramValue> 
</input> 
</inputParams> 
</billFetchRequest> ";
            string encryptedPayload = Encrypt(merchantData, workingKey);

            var url = $"https://stgapi.billavenue.com/billpay/extBillCntrl/billFetchRequest/xml" +
                  $"?accessCode={accessCode}&requestId={requestId}&ver={ver}&instituteId={instituteId}";



            using (var client = new HttpClient())
            {
                try
                {
                    var content = new StringContent(encryptedPayload, Encoding.UTF8, "text/plain"); // Body is raw text
                    var response = await client.PostAsync(url, content);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        return "";
                    }


                    var result = await response.Content.ReadAsStringAsync();

                    string decryptedResponse = Decrypt(result, workingKey);
                    // Optionally decrypt here
                    return "";
                }
                catch (HttpRequestException ex)
                {
                    return "";
                }
            }
        }

        private string GenerateRequestId()
        {
            return $"{Guid.NewGuid():N}{DateTime.Now:fff}".Substring(0, 35);
        }



        private string Encrypt(string plainText, string key)
        {
            byte[] keyBytes = HexToBytes(MD5Hash(key));
            byte[] iv = new byte[16] {
                0x00, 0x01, 0x02, 0x03,
                0x04, 0x05, 0x06, 0x07,
                0x08, 0x09, 0x0a, 0x0b,
                0x0c, 0x0d, 0x0e, 0x0f
            };

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                ICryptoTransform encryptor = aes.CreateEncryptor();

                byte[] encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                return BitConverter.ToString(encrypted).Replace("-", "").ToLower();
            }
        }

        private string Decrypt(string encryptedHex, string key)
        {
            byte[] keyBytes = HexToBytes(MD5Hash(key));
            byte[] iv = new byte[16] {
                0x00, 0x01, 0x02, 0x03,
                0x04, 0x05, 0x06, 0x07,
                0x08, 0x09, 0x0a, 0x0b,
                0x0c, 0x0d, 0x0e, 0x0f
            };

            byte[] encryptedBytes = HexToBytes(encryptedHex);

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                ICryptoTransform decryptor = aes.CreateDecryptor();
                byte[] decrypted = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                return Encoding.UTF8.GetString(decrypted);
            }
        }

        private string MD5Hash(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hash = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        private byte[] HexToBytes(string hex)
        {
            int length = hex.Length;
            byte[] result = new byte[length / 2];
            for (int i = 0; i < length; i += 2)
            {
                result[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return result;
        }




        [HttpGet]
        public IActionResult GetCategories()
        {
            return Ok(new List<object>
        {
            new { value = "Broadband Postpaid", text = "Broadband Postpaid" },
        new { value = "Cable TV", text = "Cable TV" },
        new { value = "Clubs and Associations", text = "Clubs and Associations" },
        new { value = "OU12BB000NATKB", text = "Credit Card" },
        new { value = "Donation", text = "Donation" },
        new { value = "DTH", text = "DTH" },
        new { value = "Education Fees", text = "Education Fees" },
        new { value = "Electricity", text = "Electricity" },
        new { value = "E-Challan", text = "E-Challan" },
        new { value = "Fastag", text = "Fastag" },
        new { value = "Gas", text = "Gas" },
        new { value = "Health Insurance", text = "Health Insurance" },
        new { value = "Hospital", text = "Hospital" },
        new { value = "Hospital and Pathology", text = "Hospital and Pathology" },
        new { value = "Housing Society", text = "Housing Society" },
        new { value = "Insurance", text = "Insurance" },
        new { value = "Landline Postpaid", text = "Landline Postpaid" },
        new { value = "Life Insurance", text = "Life Insurance" },
        new { value = "Loan Repayment", text = "Loan Repayment" },
        new { value = "LPG Gas", text = "LPG Gas" },
        new { value = "Mobile Postpaid", text = "Mobile Postpaid" },
        new { value = "Mobile Prepaid", text = "Mobile Prepaid" },
        new { value = "Municipal Services", text = "Municipal Services" },
        new { value = "Municipal Taxes", text = "Municipal Taxes" },
        new { value = "Recurring Deposit", text = "Recurring Deposit" },
        new { value = "Rental", text = "Rental" },
        new { value = "Subscription", text = "Subscription" },
        new { value = "Water", text = "Water" },
        new { value = "NCMC", text = "NCMC" },
        new { value = "NPS", text = "NPS" },
        new { value = "Prepaid Meter", text = "Prepaid Meter" }
        });
        }

        [HttpGet]
        public IActionResult GetLocations()
        {
            return Ok(new List<object>
        {
            new { value = "Credit Card", text = "Credit Card" }
        });
        }

        [HttpGet]
        public async Task<IActionResult> GetBillers(string category, string location)
        {
            var billers = new List<object>();

            string accessCode = "AVIV40NP24LP89LAJG";
            string requestId = GenerateRequestId();
            string workingKey = "831CA4627BD384B4112540F9E9ED0296";
            string ver = "1.0";
            string instituteId = "PF06";
            string merchantData = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<billerInfoRequest>
  <billerId>{category}</billerId>
</billerInfoRequest>";

            string apiUrl = $"https://stgapi.billavenue.com/billpay/extMdmCntrl/mdmRequestNew/xml" +
                            $"?accessCode={accessCode}&requestId={requestId}&ver={ver}&instituteId={instituteId}";

            string encryptedPayload = Encrypt(merchantData, workingKey);

            using (var client = new HttpClient())
            {
                try
                {
                    var content = new StringContent(encryptedPayload, Encoding.UTF8, "text/plain"); // Body is raw text
                    var response = await client.PostAsync(apiUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        // 🧩 Decrypt the response
                        string decryptedResponse = Decrypt(result, workingKey);
                        BillerInfoResponse billerInfo;
                        var serializer = new XmlSerializer(typeof(BillerInfoResponse));
                        using (TextReader reader = new StringReader(decryptedResponse))
                        {
                            billerInfo = (BillerInfoResponse)serializer.Deserialize(reader);
                        }

                        if (billerInfo?.Biller != null)
                        {
                            //return Ok(new
                            //{
                            //    billerName = billerInfo.Biller.billerName,
                            //    category = billerInfo.Biller.billerCategory,
                            //    alias = billerInfo.Biller.billerAliasName,
                            //    inputs = billerInfo.Biller.billerInputParams,
                            //    additionalInfo = billerInfo.Biller.billerAdditionalInfo,
                            //    paymentModes = billerInfo.Biller.billerPaymentModes,
                            //    channels = billerInfo.Biller.billerPaymentChannels
                            //});
                        }
                        if (category == "OU12BB000NATKB")
                        {
                            billers.Add(new { value = billerInfo.Biller.billerName, text = billerInfo.Biller.billerName });                           
                        }
                        else
                        {
                            billers.Add(new { value = "Vodafone", text = "Vodafone" });
                            billers.Add(new { value = "Jio", text = "Jio" });
                        }
                       

                        return Ok(billers);
                    }
                    else
                    {

                        if (category == "Credit Card")
                        {
                            billers.Add(new { value = "Vodafone", text = "Vodafone" });
                            billers.Add(new { value = "Jio", text = "Jio" });
                        }

                        return Ok(billers);
                    }
                }
                catch (Exception ex)
                {

                    if (category == "Credit Card")
                    {
                        billers.Add(new { value = "Vodafone", text = "Vodafone" });
                        billers.Add(new { value = "Jio", text = "Jio" });
                    }

                    return Ok(billers);
                }
            }
        }
    }
}
