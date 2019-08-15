from productionist import Productionist, ContentRequest

must_have_tags = {}
must_not_have_tags = {}
scoring_metric = []

request = ContentRequest(must_have=must_have_tags, must_not_have=must_not_have_tags, scoring_metric=scoring_metric)

content_bundle = "introduction"
dir = "exports"

prod = Productionist(content_bundle_name=content_bundle, content_bundle_directory=dir, probabilistic_mode=False,
                 repetition_penalty_mode=True, terse_mode=False, verbosity=1, seed=None)

result = prod.fulfill_content_request(request)
print(result)

# request = ContentRequest({"male"},{"female"},[("male",1)])

# prod=Productionist("introduction", "exports", False, True, False, 1, None)

# print(prod.fulfill_content_request(request))