namespace Company.WebApplication1.Identity.Models.ApplicationViewModels
{
    public class ChangeApplicationNameViewModel
    {
        public ChangeApplicationNameViewModel()
        {
        }

        public ChangeApplicationNameViewModel(string name)
        {
            Name = name;
        }

        public string Name { get; set; }
    }
}
